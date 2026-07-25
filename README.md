# Unity Project

Unity版のプロジェクトはこのフォルダに作成します。

Renderで動いているThree.js版はルートの `package.json`、`server.js`、`public/` を使うため、この `unity/` フォルダとは分離されています。

Unity Hubで新規プロジェクトを作成するときは、保存先をこの `unity/` フォルダにしてください。

Gitに入れる主なUnityファイル:

- `Assets/`
- `Packages/`
- `ProjectSettings/`

Gitに入れないUnity生成ファイル:

- `Library/`
- `Temp/`
- `Obj/`
- `Logs/`
- `Build/`
- `Builds/`
- `UserSettings/`

Unity WebGL版をRenderから配信したい場合は、ビルド出力を `public/unity/` に置くと、既存のThree.js版と共存できます。
