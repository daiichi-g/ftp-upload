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
        /// ディレクトリをアップロード
        /// </summary>
        /// <param name="localFolder">ローカル側のフォルダパス</param>
        /// <param name="remoteFolder">リモート側のフォルダパス</param>
        /// <returns></returns>
        public async Task<bool> UploadDirectoryAsync(string localFolder, string remoteFolder)
        {
            var success = false;
            var client = new AsyncFtpClient(Server, User, Password);
            try
            {
                var profile = await client.AutoConnect();
                var result = await client.UploadDirectory(
                    localFolder: localFolder,
                    remoteFolder: remoteFolder,
                    mode: FtpFolderSyncMode.Update,
                    existsMode: FtpRemoteExists.Overwrite
                );
                success = result.All(v => v.IsSuccess);
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
    }
}
