# rimworld-petition

RimWorld 1.7 用MOD「陳情と交渉(Petitions)」。不満が精神崩壊として表に出る前に、入植者が**具体的な要求(陳情)**として声を上げ、プレイヤーが応じるか、説得するか、却下するかを選べるようにする。

- 設計書: [docs/superpowers/specs/2026-10-22-petitions-design.md](docs/superpowers/specs/2026-10-22-petitions-design.md)
- 実装計画: [docs/superpowers/plans/2026-10-22-petitions-mvp.md](docs/superpowers/plans/2026-10-22-petitions-mvp.md)
- 対象: RimWorld **1.7**(Waylith エンジン / .NET 10)専用。1.6 には対応しない。
- 現在の段階: **MVP 完成**(設計書 §8 の段階2)。

## 遊び方の概要

1. 入植者の思考(相部屋、テーブルなしの食事、娯楽不足、放置された遺体、臓器摘出・処刑・人身売買・人の解体を知ったこと)から、不満圧が溜まる。
2. 不満圧が閾値を超えると陳情の手紙が届く。同じ不満を持つ入植者は連名で加わる。
3. 手紙か「陳情」タブで、次のいずれかを選ぶ。答えずに放置すると、2日後に「無視された」扱いになる。
   - **受諾**: 期限内の改善、または一定期間その行為をやめることを約束する。
   - **説得**: 交渉役(イデオロギーの指導者、またはいなければ交渉能力が最も高い人)が、取り下げるよう説得する。
   - **却下**
4. 却下・無視・説得の失敗・約束違反は、恨みとして溜まる。恨みの強さに応じて、段階的に悪化する。
   - 30 以上: 不平(通知)
   - 55 以上: 怠業(作業速度 ×0.85)
   - 80 以上: 最後通牒。3日以内に恨みが 70 未満にならず、その入植者への約束も進行中でなければ、あきらめて去る。
5. 怠業と最後通牒は、設定で無効化できる。陳情の頻度や不満ごとの自動応答も、設定で変えられる。

バニラの精神崩壊には手を加えていない。Harmony は不要。

## フォルダ構成

```
About/About.xml              MOD のメタデータ
LoadFolders.xml              1.7 で読み込むフォルダ(/、Common、1.7)
Common/Defs/                 不満・思考・ヘディフ・手紙・メインボタンの Def
Common/Languages/            英語(Keyed)と日本語(Keyed、DefInjected)
1.7/Assemblies/              ビルドの出力先(Git 管理外)
Source/Petitions/            MOD 本体(net10.0)
Source/Petitions/Core/       ゲームに依存しない純粋なロジック
Source/Petitions.Tests/      Core の単体テスト(xUnit v3)
tools/validate_mod_xml.py    XML・翻訳・DefOf の静的検証
tools/check_api_compat.sh    新しい 1.7 ビルドの参照でのコンパイル確認
```

## ビルドと検証

.NET 10 SDK が必要。

```sh
dotnet build Source/Petitions.slnx -c Release   # 1.7/Assemblies/Petitions.dll ができる
dotnet test --solution Source/Petitions.slnx    # Core の単体テスト
python3 -I tools/validate_mod_xml.py            # XML・翻訳・DefOf の検証
tools/check_api_compat.sh                       # 1.7.4985 の参照でもコンパイルできるか
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

> **参照パッケージのバージョンについて**: 1.7 のホスト(`RimWorld.dll`)には独自のアセンブリ解決処理がなく、MOD は .NET の標準の規則で読み込まれる。この規則では、実行中のゲームより新しい Assembly-CSharp を参照するMODは読み込めないおそれがある。そのため、参照はあえて古めの 1.7.4982 に固定し、新しいビルドで API が消えていないかは `tools/check_api_compat.sh` で確かめる。

## ゲームで試す

1. このリポジトリを RimWorld の `Mods` フォルダに置く(またはリンクを張る)。そのうえでビルドし、`1.7/Assemblies/Petitions.dll` を作る。
2. MOD 一覧で「Petitions」を有効にする。
3. 開発者モードのデバッグ操作(カテゴリ「Petitions」)を使う。

| 操作 | 内容 |
|---|---|
| `Force petition` | 選んだ入植者に、最も強い不満で陳情させる(閾値と頻度制限を無視) |
| `Fill grievance pressure` | 選んだ入植者の不満圧を、すべて閾値まで上げる |
| `Add 20 resentment` / `Clear resentment` | 恨みを増やす/0 にする |
| `Expire open petitions` | 未回答の陳情を失効させる |
| `Advance deadlines to now` | 約束と最後通牒の期限を今にして、更新を走らせる |
| `Run hourly update now` | 1時間ごとの更新をすぐに実行する |
| `Log petition state` | 状態をログに出す |

確認手順は、設計書 §7 のゲーム内手動テストの一覧を参照。

## 注意

- 既存のセーブに途中から導入できる。ただし**途中で外すのは非推奨**(独自の思考・ヘディフ・手紙がセーブに残るため)。
- ゲーム内での動作確認は、まだ実機で行っていない(開発環境に 1.7 の実行環境がないため)。ビルド、単体テスト、XML 検証、1.7.4982 と 1.7.4985 の参照でのコンパイルは通っている。
- ゲームの DLL やデコンパイルしたコードは、このリポジトリに含めない。
