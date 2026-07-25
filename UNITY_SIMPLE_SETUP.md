# Unity最小実装手順

この手順は `GameManager.cs` 1本で、Three.js版のゲーム内容をUnityに最小再現する方法です。

## 1. Unityで開く

Unity Hubで `Open` / `Add project from disk` を選び、以下のフォルダを開きます。

`/home/s1324110/hora/unity`

Unityバージョンは 2022 LTS か 2023 LTS を推奨します。初回起動時にUnityが不足している設定ファイルや `Library/` を生成します。

## 2. Mainシーンを自動生成する

Unity上部メニューから以下を実行します。

`Hora > Create Simple Scene`

これで `Assets/Scenes/Main.unity` が作られ、以下が自動配置されます。

- Player
- Main Camera
- Flashlight
- GameManager
- 神社/鳥居/鈴/札/狐
- Canvasと開始/終了UI

生成後、Playを押して開始ボタンをクリックすれば最小版を確認できます。

## 3. 手作業で作る場合

`Assets/Scenes/Main.unity` を作成して保存します。

## 4. Playerを作る

空のGameObjectを作り、名前を `Player` にします。

設定:

- Position: `(0, 1.7, 18)`
- Add Component: `CharacterController`
- CharacterController Height: `1.7`
- CharacterController Radius: `0.42`

`Player` の子に `Main Camera` を置きます。

- Local Position: `(0, 0, 0)`
- Field of View: `72`

`Main Camera` の子に `Spot Light` を置き、懐中電灯にします。

- Range: `22`
- Spot Angle: `49`
- Intensity: `11.5`

## 5. GameManagerを作る

空のGameObjectを作り、名前を `GameManager` にします。

`Assets/Scripts/GameManager.cs` をAdd Componentします。

Inspectorで以下を割り当てます。

- Player Controller: `Player`
- Player Camera: `Main Camera`
- Flashlight: `Spot Light`
- Enemy: 狐オブジェクト
- Sealed Torii: 鳥居封鎖用Cube
- Bell: 鈴オブジェクト
- Ofuda Items: 札3枚
- UI: Canvas内の各Text/Panel

## 6. 場面オブジェクトをPrimitiveで置く

最初は全部Cube/Cylinderで十分です。

主要座標:

- Bell: `(0, 2.25, -10.25)`
- Enemy: `(0, 0, -15)`
- Ofuda 1: `(-7.45, 1.6, 0.55)`
- Ofuda 2: `(7.0, 1.55, -1.35)`
- Ofuda 3: `(6.0, 1.8, 4.05)`
- Exit Torii: `(0, 0, 18)`

床:

- PlaneまたはCube
- Size目安: `80 x 80`

拝殿:

- Cubeを複数配置
- Three.js版と同じように、`z = -13` 付近に建物を置きます。

## 7. UIを作る

Canvasを作成し、以下を置きます。

- `ObjectiveText`
- `OfudaText`
- `StaminaText`
- `BlindText`
- `PromptText`
- `StartPanel`
- `EndingPanel`
- `EndingTitleText`
- `EndingBodyText`

`StartPanel` の開始ボタンには、ButtonのOnClickで以下を指定します。

- Object: `GameManager`
- Function: `GameManager.StartGame`

`EndingPanel` のもう一度ボタンには以下を指定します。

- Object: `GameManager`
- Function: `GameManager.Retry`

## 8. 操作

- WASD: 移動
- Mouse: 視点移動
- Shift: 走る
- E: 調べる/取る
- F / クリック: 懐中電灯オンオフ
- Q: 目くらまし

## 9. 最初の確認順

1. Playで開始画面が出る
2. 開始ボタンでマウスがロックされる
3. WASDで動ける
4. 鈴の近くで `E 鈴を鳴らす` が出る
5. 鈴を鳴らすと狐が出る
6. 札を3枚取ると鳥居が開く
7. 開始位置付近の鳥居に戻るとクリア
8. 狐に近づかれるとゲームオーバー
