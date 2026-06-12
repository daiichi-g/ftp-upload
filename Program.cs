using System.CommandLine;
using ftp_upload;

const string AppOfflineContent = """
<!DOCTYPE html>
<html lang="ja">
<head>
  <meta charset="utf-8">
  <title>メンテナンス中</title>
</head>
<body>
  <h1>メンテナンス中です</h1>
  <p>アプリケーションを更新しています。数分後に再度アクセスしてください。</p>
</body>
</html>
""";

var serverOption = new Option<string>("--server") { Required = true, Description = "FTPサーバー名" };
var userOption = new Option<string>("--user") { Required = true, Description = "FTPユーザー名" };
var passwordOption = new Option<string>("--password") { Required = true, Description = "FTPパスワード" };
var remoteOption = new Option<string>("--remote") { Required = true, Description = "リモートパス" };
var localOption = new Option<string>("--local") { Required = true, Description = "ローカルパス" };
var mirrorOption = new Option<bool>("--mirror") { Required = false, Description = "ミラーリングするかどうか", DefaultValueFactory = (_) => false };
var appOfflineOption = new Option<bool>("--app-offline") { Required = false, Description = "ASP.NET Core on IIS向けにapp_offline.htmを配置するかどうか", DefaultValueFactory = (_) => false };
var appOfflineWaitSecondsOption = new Option<int>("--app-offline-wait-seconds") { Required = false, Description = "app_offline.htm配置後、アップロード開始まで待機する秒数", DefaultValueFactory = (_) => 30 };

var rootCommand = new RootCommand("FTPアップロードするCLIツール");
rootCommand.Options.Add(serverOption);
rootCommand.Options.Add(userOption);
rootCommand.Options.Add(passwordOption);
rootCommand.Options.Add(remoteOption);
rootCommand.Options.Add(localOption);
rootCommand.Options.Add(mirrorOption);
rootCommand.Options.Add(appOfflineOption);
rootCommand.Options.Add(appOfflineWaitSecondsOption);

rootCommand.SetAction(async parseResult =>
{
    try
    {
        // コマンドライン引数の取得
        var server = parseResult.GetValue(serverOption) ?? "";
        var user = parseResult.GetValue(userOption) ?? "";
        var password = parseResult.GetValue(passwordOption) ?? "";
        var remote = parseResult.GetValue(remoteOption) ?? "";
        var local = parseResult.GetValue(localOption) ?? "";
        var mirror = parseResult.GetValue(mirrorOption);
        var appOffline = parseResult.GetValue(appOfflineOption);
        var appOfflineWaitSeconds = parseResult.GetValue(appOfflineWaitSecondsOption);

        Console.WriteLine("コマンドライン引数の値:");
        Console.WriteLine($"server: {server}");
        Console.WriteLine($"user: {user}");
        Console.WriteLine($"password: {password}");
        Console.WriteLine($"remote: {remote}");
        Console.WriteLine($"local: {local}");
        Console.WriteLine($"mirror: {mirror}");
        Console.WriteLine($"app-offline: {appOffline}");
        Console.WriteLine($"app-offline-wait-seconds: {appOfflineWaitSeconds}");


        // パラメータチェック
        var errors = new List<string>();
        if (server == "")
        {
            errors.Add("FTPサーバー名を指定してください。");
        }
        if (user == "")
        {
            errors.Add("FTPユーザー名を指定してください。");
        }
        if (password == "")
        {
            errors.Add("FTPパスワードを指定してください。");
        }
        if (remote == "")
        {
            errors.Add("アップロード先のディレクトリパスを指定してください。");
        }
        if (local == "")
        {
            errors.Add("リモート側のパスを指定してください。");
        }
        else if (!(Directory.Exists(local) || File.Exists(local)))
        {
            errors.Add("ローカル側のパスには、存在するファイルorディレクトリのパスを指定してください。");
        }
        else
        {
            Console.WriteLine("");
            Console.WriteLine($"Path.GetFullPath(local): {Path.GetFullPath(local)}");
        }
        if (appOfflineWaitSeconds < 0 || appOfflineWaitSeconds > 300)
        {
            errors.Add("app-offline-wait-secondsには、0〜300の範囲の秒数を指定してください。");
        }
        if (appOffline && File.Exists(local))
        {
            errors.Add("app-offlineはディレクトリアップロード時のみ利用できます。localにはディレクトリのパスを指定してください。");
        }
        if (errors.Count() > 0)
        {
            Console.WriteLine("パラメータ指定が不適切なため、FTPアップロードを中止しました。");
            Console.WriteLine($"{string.Join("\n", errors.Select(v => $"　・{v}"))}");

            throw new Exception($"パラメータ指定が不適切なため、FTPアップロードを中止しました。");
        }

        var ftp = new Ftp(server, user, password);
        if (appOffline)
        {
            var success = await RunWithAppOfflineAsync(ftp, local, remote, mirror, appOfflineWaitSeconds);
            if (!success)
            {
                return 1;
            }
        }
        else
        {
            var success = await UploadWithRetryAsync(ftp, local, remote, mirror, excludeAppOfflineFromMirror: false);
            if (!success)
            {
                throw new Exception("FTPアップロードに失敗しました");
            }
        }

        Console.WriteLine("FTPアップロードが完了しました。");
        return 0;
    }
    catch (InvalidOperationException ex)
    {
        Console.Error.WriteLine($"エラー: {ex.Message}");
        return 1;
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"予期しないエラーが発生しました: {ex.Message}");
        return 1;
    }
});

var parseResult = rootCommand.Parse(args);
return parseResult.Invoke();

static async Task<bool> RunWithAppOfflineAsync(Ftp ftp, string local, string remote, bool mirror, int appOfflineWaitSeconds)
{
    var remoteAppOfflinePath = Ftp.CombineRemotePath(remote, Ftp.AppOfflineFileName);
    var appOfflineUploadAttempted = false;
    var uploadSuccess = false;
    var deleteSuccess = false;

    Console.WriteLine($"app_offline.htm配置先: {remoteAppOfflinePath}");
    if (await ftp.RemoteFileExistsAsync(remoteAppOfflinePath))
    {
        Console.Error.WriteLine($"app_offline.htmは既に存在します: {remoteAppOfflinePath}");
        Console.Error.WriteLine("既存のメンテナンスページを上書き・削除しないため、FTPアップロードを中止しました。");
        return false;
    }

    try
    {
        var tempAppOfflinePath = await CreateAppOfflineTempFileAsync();
        try
        {
            Console.WriteLine("app_offline.htmを配置中...");
            appOfflineUploadAttempted = true;
            var appOfflineUploadSuccess = await ftp.UploadFileAsync(tempAppOfflinePath, remoteAppOfflinePath);
            if (!appOfflineUploadSuccess)
            {
                Console.Error.WriteLine("app_offline.htmの配置に失敗したため、FTPアップロードを中止しました。");
                return false;
            }
        }
        finally
        {
            DeleteTempFile(tempAppOfflinePath);
        }

        if (appOfflineWaitSeconds > 0)
        {
            Console.WriteLine($"app_offline.htm配置後、{appOfflineWaitSeconds}秒待機します。");
            await Task.Delay(TimeSpan.FromSeconds(appOfflineWaitSeconds));
        }

        uploadSuccess = await UploadWithRetryAsync(ftp, local, remote, mirror, excludeAppOfflineFromMirror: true);
        if (!uploadSuccess)
        {
            Console.Error.WriteLine("FTPアップロードに失敗しました。");
        }
    }
    finally
    {
        if (appOfflineUploadAttempted)
        {
            deleteSuccess = await ftp.DeleteRemoteFileAsync(remoteAppOfflinePath);
            if (!deleteSuccess)
            {
                Console.Error.WriteLine("app_offline.htmの削除に失敗しました。サイトがメンテナンス状態のまま残っている可能性があります。");
            }
        }
    }

    return uploadSuccess && deleteSuccess;
}

static async Task<bool> UploadWithRetryAsync(Ftp ftp, string local, string remote, bool mirror, bool excludeAppOfflineFromMirror)
{
    var count = 3; // 実行回数
    for (var num = 1; num <= count; num++)
    {
        Console.WriteLine($"FTPアップロード({num})");
        var success = await ftp.UploadAsync(local, remote, mirror, excludeAppOfflineFromMirror);
        if (success)
        {
            return true;
        }

        if (num == count)
        {
            return false;
        }

        // 失敗した場合は、少し待ってから再試行
        Console.WriteLine("FTPアップロードに失敗したため、再試行します。");
        await Task.Delay(5000); // 5秒待つ
    }

    return false;
}

static async Task<string> CreateAppOfflineTempFileAsync()
{
    var tempFilePath = Path.Combine(Path.GetTempPath(), $"ftp-upload-app_offline-{Guid.NewGuid():N}.htm");
    await File.WriteAllTextAsync(tempFilePath, AppOfflineContent);
    return tempFilePath;
}

static void DeleteTempFile(string tempFilePath)
{
    try
    {
        if (File.Exists(tempFilePath))
        {
            File.Delete(tempFilePath);
        }
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"一時ファイルの削除に失敗しました: {tempFilePath} ({ex.Message})");
    }
}
