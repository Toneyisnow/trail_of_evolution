# Trail of Mutation

Unity **6000.3.10f1**，一位真人用户对一位电脑的完整本地桌游。

## 开始游戏

使用 Unity Hub 打开本目录。打开 `Assets/Scenes/StartupScene.unity` 后按 Play。
也可以直接运行 `Builds/Windows/TrailOfMutation.exe`（构建产物不纳入 Git）。

- 首页点击 **Start** 打开 StartingModal，选择角色（FERN 鹿 / EMBER 狐狸 / TIDAL 海龟）和先手，然后点击 **BEGIN THE TRAIL**。弹窗可通过 X 或 Esc 关闭。
- **ROLL DICE** 掷骰；电脑自动掷骰与选择突变。
- 情景出现后点击牌面下方的 **Apply Event** 继续，保留无限阅读时间。
- 开局持有两张突变牌。每轮结束先抽一张进入手牌，三张中点击一张弃掉，保留两张；仅在三张牌待弃牌时，悬停卡牌才会高亮并抬起；两张手牌没有悬停或点击效果。
- **ESC / MENU** 随时暂停、继续或回首页；**RULES** 打开规则。
- 先到终点者获胜，可重新开始或回首页。

## 原稿对应

原始资料位于仓库上级的 `docs/`。

- `rules.jpg`：轮流掷骰；掷出 2 时所有角色结算情景；每轮双方抽突变、三张选弃一张，保留两张，再抽一张共享情景；不能退过起点；先到终点获胜。
- `mutation_cards.jpg`：8 张实体牌、7 种突变，其中 **Hard Shell 两张**。突变名称分别着色。弃牌在牌堆用完后洗回；角色持有的牌不参与洗牌。
- `scenario_cards.jpg`：6 张原始情景。卡牌使用水彩动物插画、扭曲木框、羊皮纸底与衬线字体，保留原规则文字和拼写；包含新增 **HARD SEEDS**。所有牌面为竖版比例，右栏下方两张手牌点击无效果；抽三留二期间，牌上方显示 Select one card to discard，点击卡牌即弃牌。
- `game_map.jpg`：重绘手绘森林地图，保留雨云、车头 Darwin、蛇蟹、小屋篱笆、两条进化序列、捷径与带脚鲨鱼。贴图铺在有厚度的 3D 棋盘上，倾斜相机呈现纵深，不使用景深模糊；内部仍使用 0–44 的逻辑位置。

| 情景 | 原文条件 | 失败退格 |
| --- | --- | --- |
| SPEED | Speed 免疫 | 4 |
| EARTHQUAKE | Hard Shell 免疫 | 2 |
| DROUGHT | Long Neck 免疫 | 3 |
| Eagle eyes | 只有 Bright Colors 受罚 | 6 |
| 原稿无标题的 hawks 卡 | Camouflage 免疫 | 1 |
| PREDATORS | Sharp Claws 免疫 | 5 |
| HARD SEEDS（新增） | Large Beak 免疫 | 3 |

新增卡牌原文：**The soft fruits are gone. Only hard seeds remain.** / **Move back 3 spaces UNLESS you have a large beak**。普通结算影响双方，电脑会把此能力纳入突变选择。

## 原稿不明确处的当前约定

1. **捷径**：经过第 18 格时可选择进入，跳至 31，消耗本次剩余移动。仅使用捷径的角色支付两张随机情景的退格数，突变不抵消这一费用。这是对地图“Cost: 2 random negative events”的解释；普通情景始终影响双方。
2. **先手**：首页选择真人或电脑先走，替代原稿按头发长度/迷信程度排序。
3. **掷出 2**：先前进两格，再结算共享情景。越过终点视为立即获胜，不要求精确点数。
4. **Large Beak**：新增 HARD SEEDS 情景赋予免疫退 3 格的能力；原稿其他卡牌保持不变。
5. **残缺文字**：SPEED 底部被照片遮掉的句尾按上下文补为 “UNLESS you are fast”；其他卡牌保留原稿语法和拼写。

## 工程结构

- `StartupScene` / `GameBoardScene`：真实场景文件，已加入 Build Settings。
- `GameRules.cs`：独立规则模型、牌库、AI 突变评估；真人用户以 `ControllerKind.Human` 区分游戏角色 `PlayerCharacter`。
- `TrailGame.cs`：界面、回合协程、骰子/飞牌/棋子动画、暂停与胜利彩纸粒子。
- `TrailUI.cs`：统一配色、矢量式 UI 组件、动态圆角素材；卡牌另使用本机 Georgia Bold 衬线字体资源。
- `TrailSmokeTest.cs`：命令行 `--trail-smoke` 执行自动完整对局，输出截图与结果日志；正常启动不启用。
- `Editor/TrailProjectTools.cs`：菜单 **Trail of Mutation** 可重新生成场景、验证规则、构建 Windows。

棋盘与 UI 由场景中的 `TrailGame` 在运行时构建，因此编辑态不会显示完整布局。所有动画由 Unity 实时驱动；胜利彩纸使用轻量 UI 粒子，不需要 Blender。画面自适应窗口，额外宽高以森林色背景补齐。

## 验证

**Trail of Mutation → Validate Rules** 检查起终点边界、49 种突变/情景组合、Large Beak 免疫及 AI 选择、双 Hard Shell、持牌排除、1000 次抽弃循环和洗牌。

**Trail of Mutation → Build Windows Player** 生成本地 Windows 版本。
可用 `TrailOfMutation.exe --trail-smoke -logFile smoke.log` 执行三局完整自动对局，包含两种先手顺序。

不会保存中途进度；回首页或关闭游戏后重新开局。

## 本次验证结果

- Unity 6000.3.10f1 Windows x64 Development Build 成功。
- `TRAIL_RULES_OK`：49 种组合、Large Beak 能力与电脑评估、边界及牌堆循环通过。
- `TRAIL_SMOKE_OK`：最终构建连续三局完成，覆盖两种先手；首页进入游戏、规则开关、暂停/继续的按钮回调检查通过，无运行时异常。
- 使用 URP 摄像机直接渲染检查首页、棋盘、HARD SEEDS、胜利彩纸画面，避免隐藏测试窗口的黑色交换链截图。该检查不替代鼠标/键盘输入的人工验收。
- 截图位于仓库 `docs/previews/`。本机日志位于 `Logs/build-render.log` 和 `Logs/smoke-render.log`（日志与构建缓存不入 Git）。

## Game Board 美术更新

地图与卡牌素材、生成提示词、3D 渲染实现及规则边界见 [美术说明](../docs/board-art-direction.md)。棋子为实时 Unity 网格组成的鹿、狐狸与海龟模型，真人动物及颜色跟随首页选择。捷径保留现有规则，具体走法仍待商议。

## 双手牌与倾斜棋盘

棋盘使用 25° 倾角及透视投影，远端自然收窄。底部状态条已移除，真人手牌移至右栏下方；两张能力同时生效，任一张满足免疫条件即可免疫，但持有 Bright Colors 时 Eagle eyes 仍触发。电脑也持有两张牌，并按剩余两张牌的共同风险决定弃牌。情景卡下方的 Apply Event 按钮结算一次后立即禁用。

布局更新：右栏宽度与手牌尺寸缩至此前的 70%，移除牌库数量。地图视口扩大并延伸到右栏后方，外围背景与整屏背景一致，不再出现额外绿框。所有卡牌木框外的橘色背景已改为透明。

## 棋盘移动与 3D 骰子

- 鼠标左键拖动地图；在地图上滚动滚轮缩放，每格约 10 个百分点，范围 100%–200%。
- 拖动、缩放和窗口大小变化后，地图的可见宽度与高度各至少保留 60%。限制以实际投影地图边界计算，不包含画布的空白背景；侧栏覆盖区域仍属于屏幕内。
- 右栏跟随屏幕边缘，右侧、顶部和底部使用相同留白。地图输入不会穿过侧栏或情景卡。
- 骰子使用圆角网格与六面真实点数，在右栏空白区域翻滚、弹跳并停稳；朝上点数与本次移动数一致。暂停时骰子动画也暂停。
