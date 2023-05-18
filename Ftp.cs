using System;
using FluentFTP;

namespace ftp_upload
{
    public class Ftp
    {
        /// <summary>
        /// FTPサーバー名
        /// </summary>
        public string Server { get; }

        /// <summary>
        /// FTPユーザー名
        /// </summary>
        public string User { get; }

        /// <summary>
        /// FTPパスワード
        /// </summary>
        public string Password { get; }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="server">FTPサーバー名</param>
        /// <param name="user">FTPユーザー名</param>
        /// <param name="password">FTPパスワード</param>
        public Ftp(string server, string user, string password)
        {
            Server = server;
            User = user;
            Password = password;
        }


        /// <summary>
        /// アップロード
        /// </summary>
        /// <param name="local">ローカル側のパス</param>
        /// <param name="remote">リモート側のパス</param>
        /// <param name="mirror">ミラーリングするかどうか</param>
        /// <returns></returns>
        public async Task<bool> UploadAsync(string local, string remote, bool mirror)
        {
            var type = CalcPathType(local);
            switch (type)
            {
                case PathType.File:
                    return await UploadFileAsync(local, remote);
                case PathType.Directory:
                    return await UploadDirectoryAsync(local, remote, mirror);
                case PathType.NotFound:
                default:
                    return false;
            }
        }

        /// <summary>
        /// ディレクトリアップロード
        /// </summary>
        /// <param name="local">ローカル側のパス</param>
        /// <param name="remote">リモート側のパス</param>
        /// <param name="mirror">ミラーリングするかどうか</param>
        /// <returns></returns>
        public async Task<bool> UploadDirectoryAsync(string local, string remote, bool mirror)
        {
            var success = false;
            var client = new AsyncFtpClient(Server, User, Password);
            try
            {
                Console.WriteLine();
                Console.WriteLine("FTP接続中...");
                Console.WriteLine();
                var profile = await client.AutoConnect();

                Console.WriteLine();
                Console.WriteLine("ディレクトリアップロード中...");
                Console.WriteLine();

                var results = await client.UploadDirectory(
                    localFolder: local,
                    remoteFolder: remote,
                    mode: mirror ? FtpFolderSyncMode.Mirror : FtpFolderSyncMode.Update,
                    existsMode: FtpRemoteExists.Overwrite
                );
                success = results.All(v => !v.IsFailed);

                // アップロードファイルをログ出力
                foreach (var result in results)
                {
                    Console.ForegroundColor = result.IsFailed ? ConsoleColor.Red : ConsoleColor.Green;
                    Console.WriteLine(
                        string.Join("\t", new string[] {
                            $"{results.IndexOf(result) + 1}/{results.Count()}",
                            CalcStatusLabel(result),
                            GetResultType(result.Type),
                            GetFormatFileSize(result),
                            result.RemotePath,
                        })
                    );
                    Console.ResetColor();
                }
                Console.WriteLine();

                Console.ForegroundColor = success ? ConsoleColor.Green : ConsoleColor.Red;
                Console.WriteLine($"結果(OK:{results.Count(v => !v.IsFailed)}, NG:{results.Count(v => v.IsFailed)})");
                Console.ResetColor();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"FtpHelper.Upload ex: {ex.Message}");
            }
            finally
            {
                await client.Disconnect();
            }
            return success;
        }



        /// <summary>
        /// ファイルアップロード
        /// </summary>
        /// <param name="local">ローカル側のパス</param>
        /// <param name="remote">リモート側のパス</param>
        /// <param name="mirror">ミラーリングするかどうか</param>
        /// <returns></returns>
        public async Task<bool> UploadFileAsync(string local, string remote)
        {
            var success = false;
            var client = new AsyncFtpClient(Server, User, Password);
            try
            {
                Console.WriteLine();
                Console.WriteLine("FTP接続中...");
                Console.WriteLine();
                var profile = await client.AutoConnect();

                Console.WriteLine();
                Console.WriteLine("ファイルアップロード中...");
                Console.WriteLine();

                var status = await client.UploadFile(
                    localPath: local,
                    remotePath: remote,
                    existsMode: FtpRemoteExists.Overwrite
                );


                // アップロードファイルをログ出力
                Console.ForegroundColor = status == FtpStatus.Failed ? ConsoleColor.Red : ConsoleColor.Green;
                Console.WriteLine(
                    string.Join("\t", new string[] {
                            CalcStatusLabel(status),
                            GetFormatFileSize(new FileInfo(local).Length),
                            $"{local} → {remote}",
                    })
                );
                Console.ResetColor();

                Console.WriteLine();

                Console.ForegroundColor = CalcConsoleColor(status);
                Console.WriteLine($"結果: {CalcStatusLabel(status)}");
                Console.ResetColor();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"FtpHelper.Upload ex: {ex.Message}");
            }
            finally
            {
                await client.Disconnect();
            }
            return success;
        }

        private static string CalcStatusLabel(FtpStatus value)
        {
            switch (value)
            {
                case FtpStatus.Success:
                    return "OK";
                case FtpStatus.Skipped:
                    return "SKIP";
                case FtpStatus.Failed:
                    return "NG";
                default:
                    return "UNKNOWN";
            }
        }
        private static string CalcStatusLabel(FtpResult value)
        {
            if (value.IsSkipped)
            {
                return "SKIP";
            }
            if (value.IsFailed)
            {
                return "NG";
            }
            return "OK";
        }


        private static ConsoleColor CalcConsoleColor(FtpStatus value)
        {
            switch (value)
            {
                case FtpStatus.Success:
                    return ConsoleColor.Green;
                case FtpStatus.Skipped:
                    return ConsoleColor.DarkYellow;
                case FtpStatus.Failed:
                    return ConsoleColor.Red;
                default:
                    return ConsoleColor.White;
            }
        }
        private static ConsoleColor CalcConsoleColor(FtpResult value)
        {
            if (value.IsSkipped)
            {
                return ConsoleColor.DarkYellow;
            }
            if (value.IsFailed)
            {
                return ConsoleColor.Red;
            }
            return ConsoleColor.Green;
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        private static string GetResultType(FtpObjectType value)
        {
            switch (value)
            {
                case FtpObjectType.Directory:
                    return "DIR";
                case FtpObjectType.File:
                    return "FILE";
                case FtpObjectType.Link:
                    return "LINK";
                default:
                    return "UNKNOWN";
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="result"></param>
        /// <param name="totalWidth"></param>
        /// <returns></returns>
        private static string GetFormatFileSize(FtpResult result, int totalWidth = 10)
        {
            if (result.Type == FtpObjectType.File)
            {
                return GetFormatFileSize(result.Size, totalWidth);
            }


            // ファイル以外
            return "".PadLeft(totalWidth, ' ');
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="size"></param>
        /// <param name="totalWidth"></param>
        /// <returns></returns>
        private static string GetFormatFileSize(long size, int totalWidth = 10)
        {
            // ファイルサイズを適切な単位に変換
            var unit = new[] { "B", "KB", "MB", "GB", "TB" };
            var index = 0;
            while (size >= 1024)
            {
                size /= 1024;
                index++;
            }
            return $"{size}[{unit[index]}]".PadLeft(totalWidth, ' ');
        }

        private enum PathType { File, Directory, NotFound }

        /// <summary>
        /// 指定されたパスがファイルかディレクトリかを判定
        /// </summary>
        /// <param name="path">パス</param>
        /// <returns></returns>
        private static PathType CalcPathType(string path)
        {
            if (File.Exists(path))
            {
                return PathType.File;
            }

            if (Directory.Exists(path))
            {
                return PathType.Directory;
            }

            return PathType.NotFound;
        }
    }
}
