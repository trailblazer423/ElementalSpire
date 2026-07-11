# Codex 接力说明（2026-07-11）

## 一、当前工作位置

- 独立集成克隆：`F:\original e\新建文件夹\slay the spire\tmp\elementalspire_full_feature_work_20260711_v2`
- 当前分支：`integration/full-loop-20260711`
- 基线提交：`16d3670c4b61f22581a81f7436e002cd890aca75`
- 正式安装目标：`F:\unity\project\ElementalSpire`
- 用户要求：最终只安装到本机正式路径，暂时不要 push GitHub。

正式目标在本次集成开始前已同步到 `16d3670`。所有功能修改目前仍只在独立集成克隆里，尚未覆盖正式目标。

## 二、已完成的实现

1. 固定十节点单向路线：
   - 1–2 战斗
   - 3 事件
   - 4 休息
   - 5–7 战斗
   - 8 事件
   - 9 休息
   - 10 普通战斗（临时替代 Boss）
2. 地图节点双重防重复：必须已解锁、未完成、且等于第一个未完成节点。
3. 新增 `EventScene`：获得战斗同源奖励牌或按牌组实际索引删一张牌；重复牌不会合并。
4. 新增 `RestScene`：回血 15（不超过上限）或从实际牌组选择一张可升级牌；升级序列化为 `cardId+`。
5. 新增全局 HUD：非主菜单场景左上 HP、右上齿轮、齿轮左侧牌组按钮；白色“是/否”退出确认框。
6. 战斗场景新增可点击抽牌堆/弃牌堆查看；永久牌组、抽牌堆、弃牌堆均复用现有 `CardView`。
7. 新增安全点存档：`RunSaveData.cs`、`RunSaveRepository.cs`，JSON 临时文件加原子替换；不保存战斗现场。
8. 新增继续游戏恢复：HP、最大 HP、牌组、元素、节点进度、挑战开始与有效游玩时长。
9. 新增有效时长计时：主菜单、暂停、失焦/退出期间不累计；最终胜/负才落排行榜记录。
10. 新增排行榜动态 UI：开始时间、有效时长、最高进度（x/10）、结果。
11. 主菜单已接线开始游戏、继续游戏和排行榜入口。
12. 已使用用户给定图片生成休息场景背景：
    `Assets/Prefabs/Image/关卡背景/RestSceneBackground.png`
13. 已写过程文档：`Docs/full-loop-integration-process.md`。

## 三、已通过的验证

### 场景接线

- 日志：`F:\original e\新建文件夹\slay the spire\tmp\unity-full-loop-integrator-rerun.log`
- 结果：`[FullLoopSceneIntegrator] 十节点流程场景已生成并接线完成。`
- Unity 返回码：0。

### 结构验证

- 日志：`F:\original e\新建文件夹\slay the spire\tmp\unity-full-loop-validation.log`
- 结果：`[FullLoopValidation] PASS: scenes, ten-node route, controllers, build order and card codec.`
- 已验证十节点类型、场景顺序、Event/Rest 控制器、休息背景和卡牌序列化。

### Play Mode 冒烟测试进度

1. 第一轮真实发现删牌 UI 生成 0 张牌：`ClearOwnedSelectionUi()` 错误清空 `_ownedOptions`。
2. 已修复为 UI 清理与候选数据清理分离。
3. 第二轮已通过：新游戏、安全点、HUD、白色退出框、战斗中退出与继续恢复、暂停时间排除、禁止重复挑战、事件奖励跳过、休息回血、事件删牌、休息升级。
4. 第二轮最终在排行榜 SQLite 初始化处失败，错误为：
   `InvalidProgramException: Mono.Data.Sqlite.SqliteConnection.CreateCommand IL_0000: ret`。
5. 日志：`F:\original e\新建文件夹\slay the spire\tmp\unity-full-loop-runtime-smoke-rerun.log`。

## 四、当前唯一主要技术问题：SQLite 插件

根因已确认：

- 原项目 `Assets/Plugins/Mono.Data.Sqlite.dll` 是 Unity `lib/mono/unity` 的桩程序集，112640 bytes；`CreateCommand()` 运行时是非法 IL。
- 已替换为 Unity 2022.3.62f3 自带的 `unityjit-win32` 可执行程序集：
  - 166912 bytes
  - SHA-256 `91914602F682E0D4278DEC0C1F29E3A781FC63C8C3FD7DC864E6AD3F9B95FD38`
- 已把 `Mono.Data.Sqlite.dll.meta` 的 Editor 开关由 0 改为 1。
- 已加入 SQLite 3.50.4 Windows x64 原生库：
  - `Assets/Plugins/sqlite3.dll`
  - SHA-256 `BB00C81138AD27581BEE5F37AA0A225E22BAEE0EF67AD92F5726081C7140BFC9`
- 同一原生 DLL 目前还非破坏性地保留了一份在 `Assets/Plugins/x86_64/sqlite3.dll`，下一次 Unity 导入确认哪一份被正确识别后只保留一份并提交对应 `.meta`。
- `Assets/Plugins/SQLite-NOTICE.txt` 已记录来源和 SQLite 公共领域声明。

注意：Unity 自带 `MonoBleedingEdge/bin/mono.exe` 是 32 位，不能加载这份 x64 sqlite3.dll（错误 193）；实际 Unity 编辑器 `Unity.exe` 是 64 位，因此必须以 64 位 Unity Play Mode 作为最终权威验证，不能用该 32 位命令行探针判定失败。

## 五、下一步（按顺序执行）

1. 完成两个严格存档边界：
   - `MapManager.OnNodeClicked`：进入任何节点前 `SaveSafePoint()` 失败则不切场景，并回滚临时 currentNode 字段。
   - `RunHudController.ConfirmExitToMainMenu`：`UpdateChallengeStateOnly()` 返回 false 时不要退出；恢复计时并提示重试。
   - `RunFlowCoordinator`：节点完成存档失败时不要无提示回地图，应保留当前场景并提示/重试，或在回滚节点状态后明确报错。
2. 解决 Battle UI 与全局 HUD 层级：确保 `GlobalRunHudRoot` 在战斗动态 UI 创建后仍为最后 sibling，或为 HUD 使用 overrideSorting 的高排序 Canvas。
3. 扩充 `FullLoopRuntimeSmokeProbe`：实际点击牌组/抽牌堆/弃牌堆并确认 CardView；尽量验证主菜单 Continue/Ranking 按钮；验证普通胜负入口。
4. 用 Unity 2022.3.62f3 再跑一次完整 Play Mode smoke（不要加 `-quit`，Launcher 会自行退出）：

   ```powershell
   $unity = 'C:\Program Files\Unity\Hub\Editor\2022.3.62f3\Editor\Unity.exe'
   $project = 'F:\original e\新建文件夹\slay the spire\tmp\elementalspire_full_feature_work_20260711_v2'
   $log = 'F:\original e\新建文件夹\slay the spire\tmp\unity-full-loop-runtime-smoke-final.log'
   $argumentString = '-batchmode -nographics -projectPath "' + $project + '" -executeMethod FullLoopRuntimeSmokeLauncher.Run -logFile "' + $log + '"'
   Start-Process -FilePath $unity -ArgumentList $argumentString -WindowStyle Hidden -Wait
   ```

5. 确认结果文件 `full-loop-runtime-smoke-result.txt` 首行为 `PASS`，并确认日志包含 `[FullLoopRuntimeSmoke] PASS`。
6. 最终 Unity 编译、`git diff --check`、清理动态 TMP 字体非预期改动和临时测试结果。
7. 在独立克隆本地提交（不 push）。
8. 重新检查正式目标是否干净且仍在 `16d3670`，然后从独立克隆 fetch/cherry-pick 本地提交到：
   `F:\unity\project\ElementalSpire`。
9. 在正式目标再跑一次 Unity 最终编译，确认干净后交付。

## 六、不要做的事

- 不要 push GitHub。
- 不要直接覆盖正式目标的用户未提交改动。
- 不要把 `Library/`、临时 smoke 结果或测试数据库提交。
- 不要恢复战斗中手牌、敌人 HP 等现场；本需求明确采用战斗前地图安全点。
- 不要把第 10 关完成后重新解锁第 1 关。

## 七、最新最终状态（覆盖上面的 SQLite 待办说明）

- SQLite 原生组合在终局写记录时触发 Unity `SIGSEGV`，已停止使用。
- `ChallengeRecordRepository` 已改为原子写入 `challenge_records.json`，接口和排行榜排序不变。
- SQLite 实验 DLL 已从待提交内容中清理，原项目的托管 DLL 已恢复原哈希。
- 三处保存失败边界、HUD 高排序 Canvas、实际牌堆点击和普通胜负测试都已补齐。
- 最终 Play Mode 日志：
  `F:\original e\新建文件夹\slay the spire\tmp\unity-full-loop-runtime-smoke-json-final.log`
- 最终结果：`[FullLoopRuntimeSmoke] PASS`。
- 接下来只需最终编译、清理/本地提交、安装到正式路径并在正式路径再编译一次；仍然不要 push。
