[![ci](https://github.com/daiichi-g/ftp-upload/actions/workflows/ci.yml/badge.svg)](https://github.com/daiichi-g/ftp-upload/actions/workflows/ci.yml)
[![release](https://github.com/daiichi-g/ftp-upload/actions/workflows/release.yml/badge.svg)](https://github.com/daiichi-g/ftp-upload/actions/workflows/release.yml)

# FTPアップロードするアクション

## 使い方

#### ファイルアップロード
```yml
jobs:
  job:
    steps:
      - name: FTPアップロード
        uses: daiichi-g/ftp-upload@v1
        with:
          server: <FTPサーバー名>
          user: <FTPユーザー名>
          password: <FTPパスワード>
          remote: <リモート側のファイルパス>
          local: <ローカル側のファイルパス>
```

#### ディレクトリアップロード(ミラーリングなし)
```yml
jobs:
  job:
    steps:
      - name: FTPアップロード
        uses: daiichi-g/ftp-upload@v1
        with:
          server: <FTPサーバー名>
          user: <FTPユーザー名>
          password: <FTPパスワード>
          remote: <リモート側のディレクトリパス>
          local: <ローカル側のディレクトリパス>
```

#### ディレクトリアップロード(ミラーリングあり)
```yml
jobs:
  job:
    steps:
      - name: FTPアップロード
        uses: daiichi-g/ftp-upload@v1
        with:
          server: <FTPサーバー名>
          user: <FTPユーザー名>
          password: <FTPパスワード>
          remote: <リモート側のディレクトリパス>
          local: <ローカル側のディレクトリパス>
          mirror: true
```

#### ASP.NET Core on IIS向け app_offline.htm 配置あり
```yml
jobs:
  job:
    steps:
      - name: FTPアップロード
        uses: daiichi-g/ftp-upload@v1
        with:
          server: ${{ secrets.FTP_SERVER }}
          user: ${{ secrets.FTP_USER }}
          password: ${{ secrets.FTP_PASSWORD }}
          remote: /web/html/shoron/guidance/reservation-dev
          local: ./manage/bin/Release/net10.0/publish
          mirror: false
          app-offline: true
          app-offline-wait-seconds: 30
```

## パラメータ
| パラメータ名 | 必須 | デフォルト値 | 説明 |
|:---|:---:|:---:|:---|
|server  |必須  |  | FTPサーバー名  |
|user  |必須  | | FTPユーザー名  |
|password  |必須  |  | FTPパスワード  |
|remote  |必須  |  | リモート側のファイルパス(またはディレクトリパス)<span style='color:red'>※1</span> |
|local  |必須  |  | ローカル側のファイルパス(またはディレクトリパス)<span style='color:red'>※1</span> |
|mirror  | | false | true:ミラーリングあり<br>false:ミラーリングなし<br>※ディレクトリアップロード時にのみ有効なオプション |
|app-offline  | | false | true:ASP.NET Core on IIS向けに、アップロード前に`remote/app_offline.htm`を配置してアプリケーションを停止する<br>false:配置しない<br>※ディレクトリアップロード時にのみ有効なオプション |
|app-offline-wait-seconds  | | 30 | `app_offline.htm`配置後、アップロード開始まで待機する秒数<br>0〜300の範囲で指定 |

<span style='color:red'>※1: local=ファイルパスとremote=ディレクトリパス、またはその逆の組み合わせは指定できません<br>

## app_offline.htm について

`app-offline: true`を指定すると、ASP.NET Core on IIS向けにアップロード前へ`app_offline.htm`を配置し、アプリケーションを停止してからディレクトリアップロードします。配置先は`remote/app_offline.htm`固定です。

処理順は、`remote/app_offline.htm`の存在確認、runnerの一時ディレクトリでの`app_offline.htm`作成、FTP配置、`app-offline-wait-seconds`秒待機、通常のディレクトリアップロード、`app_offline.htm`削除です。通常のディレクトリアップロードでは、既存の`web.config`先行アップロード処理もそのまま実行されます。

`remote/app_offline.htm`が既に存在する場合は、既存のメンテナンスページを上書き・削除しないためエラーになります。`local`がファイルの場合、`app-offline`は利用できません。

`mirror: true`と`app-offline: true`は併用できます。本体アップロード中は`app_offline.htm`を除外し、Mirrorによる削除対象にならないようにします。
