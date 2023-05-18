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

## パラメータ
| パラメータ名 | 必須 | デフォルト値 | 説明 |
|:---|:---:|:---:|:---|
|server  |必須  |  | FTPサーバー名  |
|user  |必須  | | FTPユーザー名  |
|password  |必須  |  | FTPパスワード  |
|remote  |必須  |  | リモート側のファイルパス(またはディレクトリパス)<span style='color:red'>※1</span> |
|local  |必須  |  | ローカル側のファイルパス(またはディレクトリパス)<span style='color:red'>※1</span> |
|mirror  | | false | true:ミラーリングあり<br>false:ミラーリングなし<br>※ディレクトリアップロード時にのみ有効なオプション |

<span style='color:red'>※1: local=ファイルパスとremote=ディレクトリパス、またはその逆の組み合わせは指定できません<br>
