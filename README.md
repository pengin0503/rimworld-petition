# rimworld-petition

RimWorld 1.7 用MOD「陳情と交渉(Petitions)」。不満が精神崩壊として表に出る前に、入植者が**具体的な要求(陳情)**として声を上げ、プレイヤーが応じるか、説得するか、却下するかを選べるようにする。

- 対象: RimWorld **1.7**(Waylith エンジン / .NET 10)専用。1.6 には対応しない。
- 現在の段階: **MVP と段階3a まで完成**(設計書 §8・§10)。残りの拡張(陳情者が交渉役のところへ歩いて行く行動など)は見送り中。

## 遊び方の概要

1. 入植者の思考(相部屋、テーブルなしの食事、娯楽不足、放置された遺体、臓器摘出・処刑・人身売買・人の解体を知ったこと)から、不満圧が溜まる。イデオロギーの戒律が与える思考も、同じ出来事(HistoryEventDef)に結び付けて数える。
2. 不満圧が閾値を超えると陳情の手紙が届く。同じ不満を持つ入植者は連名で加わる。
3. 手紙か「陳情」タブで、次のいずれかを選ぶ。答えずに放置すると、2日後に「無視された」扱いになる。
   - **受諾**: 期限内の改善、または一定期間その行為をやめることを約束する。
   - **説得**: 交渉役が、取り下げるよう説得する。交渉役は陳情タブで指名できる。「自動」では、イデオロギーの指導者、いなければ交渉能力が最も高い人になる。
   - **却下**
4. 却下・無視・説得の失敗・約束違反は、恨みとして溜まる。恨みの強さに応じて、段階的に悪化する。
   - 30 以上: 不平(通知)
   - 55 以上: 怠業(作業速度 ×0.85)
   - 80 以上: 最後通牒。3日以内に恨みが 70 未満にならず、その入植者への約束も進行中でなければ、あきらめて去る。
5. 怠業と最後通牒は、設定で無効化できる。陳情の頻度や不満ごとの自動応答も、設定で変えられる。

バニラの精神崩壊には手を加えていない。Harmony は不要。

## 導入

1. [Releases](https://github.com/pengin0503/rimworld-petition/releases) から `Petitions-<版>.zip` を取得する。リリースがまだない場合は、[CI](https://github.com/pengin0503/rimworld-petition/actions/workflows/ci.yml) の成功した実行の artifact `Petitions-dev` を使う(GitHub へのログインが必要)。GitHub が自動で付ける「Source code」は MOD 本体ではない。
2. ZIP を展開し、中の `Petitions` フォルダを RimWorld の `Mods` フォルダに置く。`Mods/Petitions/About/About.xml` になっていることを確認する。
3. MOD 一覧で「Petitions」を有効にする。セーブはバックアップしてから試す。

配布 ZIP には `About/`・`LoadFolders.xml`・`Common/`・`1.7/Assemblies/Petitions.dll` だけを入れる(`tools/package_mod.py`)。ソース・テスト・ゲームの DLL は含めない。

## 注意

- 既存のセーブに途中から導入できる。ただし**途中で外すのは非推奨**(独自の思考・ヘディフ・手紙がセーブに残るため)。
- ゲーム内での動作確認は、まだ実機で行っていない(開発環境に 1.7 の実行環境がないため)。ビルド、単体テスト、XML 検証、1.7.4982 と 1.7.4985 の参照でのコンパイルは通っている。
- ゲームの DLL やデコンパイルしたコードは、このリポジトリに含めない。

## English

Petitions adds colonist requests and negotiations to RimWorld 1.7. Colonists build up grievances from conditions and events, then may raise a petition. Players can accept a promise, persuade the petitioners or reject them. Unanswered petitions are treated as ignored after two days. Rejection, failed persuasion and broken promises increase resentment, potentially leading to complaints, slower work or an ultimatum and departure. Slowdowns, ultimatums, frequency and automatic responses are configurable. Vanilla mental breaks are unchanged; Harmony is not required.

Download `Petitions-<version>.zip` from [Releases](https://github.com/pengin0503/rimworld-petition/releases), extract the `Petitions` folder into RimWorld's `Mods` folder, and enable it. If no release is available, the successful CI artifact `Petitions-dev` can be used (GitHub sign-in may be required); GitHub's automatic source archive is not the mod package.

The mod can be added to an existing save, but removing it mid-save is discouraged because its thoughts, hediffs and letters may remain. Back up saves. In-game operation has not yet been verified; passing build and automated checks does not establish gameplay compatibility.
