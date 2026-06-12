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
          app-offline-wait-seconds: '3,5,15'
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
|app-offline-wait-seconds  | | 3,5,15 | `app_offline.htm`配置後およびアップロード失敗後の待機秒数パターン<br>カンマ区切りで1〜3個、各値は0〜300の範囲で指定 |

<span style='color:red'>※1: local=ファイルパスとremote=ディレクトリパス、またはその逆の組み合わせは指定できません<br>

## app_offline.htm について

`app-offline: true`を指定すると、ASP.NET Core on IIS向けにアップロード前へ`app_offline.htm`を配置し、アプリケーションを停止してからディレクトリアップロードします。配置先は`remote/app_offline.htm`固定です。

処理順は、`remote/app_offline.htm`の存在確認、runnerの一時ディレクトリでの`app_offline.htm`作成、FTP配置、`app-offline-wait-seconds`の待機パターンに沿ったアップロード試行、`app_offline.htm`削除です。

`app-offline: false`のディレクトリアップロードでは、`local/web.config`が存在する場合のみ先行アップロードと3秒待機を行ってから本体アップロードを実行します。

`app-offline-wait-seconds`はカンマ区切りの待機パターンです。例えば`'3,5,15'`を指定した場合、`app_offline.htm`配置後に3秒待機して1回目のアップロードを実行し、失敗した場合は5秒待機して2回目、それでも失敗した場合は15秒待機して3回目を実行します。待機秒数の個数がアップロード試行回数になるため、`'3'`は最大1回、`'3,5'`は最大2回、`'3,5,15'`は最大3回アップロードします。`app-offline: true`の場合、既にアプリケーション停止を行っているため、`web.config`の先行アップロード処理は実行しません。

`app-offline: false`の場合、`app-offline-wait-seconds`は使用せず、既存どおり最大3回アップロードし、失敗時は5秒待機して再試行します。ディレクトリアップロード時は、`local/web.config`が存在する場合に`remote/web.config`への先行アップロードと3秒待機を行ってから本体ディレクトリアップロードを実行します。

`remote/app_offline.htm`が既に存在する場合は、既存のメンテナンスページを上書き・削除しないためエラーになります。`local`がファイルの場合、`app-offline`は利用できません。

`mirror: true`と`app-offline: true`は併用できます。本体アップロード中は`app_offline.htm`を除外し、Mirrorによる削除対象にならないようにします。
