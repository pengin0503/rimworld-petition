# rimworld-petition

RimWorld 1.7 用MOD「陳情と交渉(Petitions)」。不満が精神崩壊として表に出る前に、入植者が**具体的な要求(陳情)**として声を上げ、プレイヤーが応じるか、説得するか、却下するかを選べるようにする。

- 設計書: [docs/superpowers/specs/2026-10-22-petitions-design.md](docs/superpowers/specs/2026-10-22-petitions-design.md)
- 対象: RimWorld **1.7**(Waylith エンジン / .NET 10)専用。1.6 には対応しない。
- 現在の段階: **骨組み**(設計書 §8 の段階1)。入植者の思考から不満圧が溜まる様子を、陳情タブで確認できる。陳情、交渉、エスカレーションは次の段階で実装する。

## フォルダ構成

```
About/About.xml              MOD のメタデータ
LoadFolders.xml              1.7 で読み込むフォルダ(/、Common、1.7)
Common/Defs/                 GrievanceDef(不満の定義)、メインボタン
Common/Languages/            英語(Keyed)と日本語(Keyed、DefInjected)
1.7/Assemblies/              ビルドの出力先(Git 管理外)
Source/Petitions/            MOD 本体(net10.0)
Source/Petitions/Core/       ゲームに依存しない純粋なロジック
Source/Petitions.Tests/      Core の単体テスト(xUnit v3)
tools/validate_mod_xml.py    XML の静的検証
```

## ビルド

.NET 10 SDK が必要。

```sh
dotnet build Source/Petitions.slnx -c Release   # 1.7/Assemblies/Petitions.dll ができる
dotnet test --solution Source/Petitions.slnx    # Core の単体テスト
python3 -I tools/validate_mod_xml.py            # XML の検証
```

- ゲーム本体の DLL は、NuGet の参照パッケージ `Krafs.Rimworld.Ref` **1.7.4982-unstable**(Assembly-CSharp 1.7.9787.38423)から取得する。ゲームがインストールされていなくてもビルドできる。
- 手元のゲームの DLL でビルドする場合は、`Assembly-CSharp.dll`・`UnityStubs.dll`・`Waylith.dll` があるフォルダを指定する。

  ```sh
  dotnet build Source/Petitions/Petitions.csproj -c Release -p:RimWorldManagedDir=/path/to/dlls
  ```

- XML 検証で、参照している思考がゲームに実在するかまで確かめる場合は、ゲームの `Data/Core/Defs` を渡す。

  ```sh
  python3 -I tools/validate_mod_xml.py --core /path/to/RimWorld/Data/Core/Defs
  ```

> **参照パッケージのバージョンについて**: .NET では、実行中のゲームより新しい Assembly-CSharp を参照するMODは読み込めないおそれがある。参照パッケージを上げてよいのは、対応するゲームの最低ビルドを上げるときだけ。

## ゲームで試す

1. このリポジトリを RimWorld の `Mods` フォルダに置く(またはリンクを張る)。そのうえでビルドし、`1.7/Assemblies/Petitions.dll` を作る。
2. MOD 一覧で「Petitions」を有効にする。Harmony は不要。
3. 開発者モードのデバッグ操作(カテゴリ「Petitions」)を使う。
   - `Fill grievance pressure`: 選んだ入植者のすべての不満圧を閾値まで上げる
   - `Run hourly update now`: 1時間ごとの更新をすぐに実行する
   - `Log grievance state`: 全入植者の不満圧をログに出す
4. 画面下の「陳情」タブで、入植者ごとの不満圧を確認する。

## 注意

- 既存のセーブに途中から導入できる。ただし、**途中で外すのは非推奨**(今後のバージョンで、独自の思考・ヘディフ・手紙をセーブに書き込むため)。
- ゲームの DLL やデコンパイルしたコードは、このリポジトリに含めない。
