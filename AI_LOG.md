# AI 使用记录

按当前对话实际顺序记录，没有把 AI 的工具调用、内部检查或虚构的追问冒充用户 Prompt。后续提问继续顺序追加并保留原文。此前与本题无关的账号管理指令不纳入本题日志。

## P-001

- 目标：完成 Unity AR、Go、React 三端园区巡检原型，以及说明文档与验证记录。

### Prompt 原文

> 请辅助我完成一个Unity-AR研发实操题。1. 项目背景：园区巡检人员发现墙面破损、设备异常或安全隐患时，希望用手机在现场放置一个 AR 标记并上报问题。管理人员随后在电脑网页中查看这些问题并更新处理状态。请完成一个小型原型，打通以下流程：Unity AR 放置标记并上报   → Go 后端保存   → React 页面查看问题   → React 修改处理状态   2. 固定技术栈：移动端使用Unity LTS、C#、AR Foundation，来实现检测平面、放置标记、上报问题；管理端使用React、TypeScript来实现查看问题、修改状态；后端使用Go来提供 API 并持久化数据；其中Unity 可选择 Android + ARCore 或 iOS + ARKit；Go 框架不限；数据保存到本地 JSON 文件；但不要求产品本身接入大模型。3. 必做功能：3.1 Unity AR 移动端：在支持 AR 的真机上运行；检测真实环境中的水平平面；点击检测到的平面，在命中位置放置一个简单标记；填写并提交问题：a:标题：必填；b:描述：可选；c:优先级:`low`、`medium`、`high`; d:位置：标记的 `x/y/z` 坐标;  提交成功或失败时，给用户明确提示；点击按钮或输入框时，不应误在 AR 场景中放置标记。 3.2 React 管理端: 从 Go 后端获取问题列表；至少显示标题、优先级和状态；能将状态修改为：`open`、`in_progress`、`resolved`；后端不可用时，页面应显示错误提示，不能直接白屏。3.3 Go 后端：Go 后端需要为 Unity 和 React 提供以下能力：提供简单的服务运行状态检查；接收并保存 Unity 上报的问题；返回已经保存的问题列表；根据问题 ID 修改处理状态；为新问题生成唯一 ID，默认状态为 `open`；标题为空或枚举值错误时，返回明确的参数错误；数据在 Go 服务重启后仍然存在；Unity 和 React 使用同一个 Go 后端和同一份数据；React 在浏览器中能够正常访问后端。接口路径和响应格式可以自行设计，请在 README 中简要说明。4. 基础数据结构：三端字段名称应保持一致：{
> &#x20; "id": "由后端生成",
> &#x20; "title": "入口墙面破损",
> &#x20; "description": "左侧墙体存在裂缝",
> &#x20; "priority": "high",
> &#x20; "status": "open",
> &#x20; "position": {
> &#x20;   "x": 0.42,
> &#x20;   "y": 0.03,
> &#x20;   "z": 1.26
> &#x20; },
> &#x20; "createdAt": "由后端生成",
> &#x20; "updatedAt": "由后端生成"
> }不要求应用重启后，把历史标记恢复到完全相同的真实物理位置。5. AI 使用记录：请在项目根目录提交 `AI_LOG.md`，按实际顺序保存本题中所有由你主动发给 AI 的问题或指令，包括追问和纠错。每条记录使用以下简化格式即可：## P-001  - 目标：解决什么问题  ### Prompt 原文  > 实际发送给 AI 的问题或指令  ### 结果  - 采用 / 修改后采用 / 未采用 - 简要说明如何验证    要求：必须保留 Prompt 原文，不能只写总结；不需要记录 IDE 的逐字自动补全；至少记录一次检查、修改或放弃 AI 结果的过程；不得提交 API Key、Token、账号、个人隐私或其他敏感信息；如需脱敏，请使用类似 `[REDACTED: API token]` 的占位内容。

### 结果

- 修改后采用（工作区实现；待用户检查确认，未声称用户已验收）。
- 采用 Android + ARCore，Go 标准库与本地 JSON，React + TypeScript。
- 实际检查：对生成的后端执行 6 组自动化测试及 `go vet`；对管理端执行 TypeScript 检查和 Vite 生产构建，均通过。具体范围见 `docs/VALIDATION.md`。
- 检查/修正过程 1：检查 Unity 相机与触摸的输入依赖，发现 Input System TrackedPoseDriver 与旧版触摸、uGUI 并用需要 Both。场景生成脚本加入相应配置，并在 README 中要求核对及重启。此项仅完成源码检查，尚无 Unity 编译/真机证据。
- 检查/修正过程 2：首次 Go 测试在沙箱默认临时目录发生 Access is denied，多个持久化用例失败。没有删除失败用例或改成仅内存保存，而是将测试临时目录移至工作区内 `.tools/test-temp` 后重新运行，6 组测试通过。
- 检查/修正过程 3：本机 npm 出现 `Class extends value undefined is not a constructor or null`；弃用该损坏的本机 npm 执行路径，使用应用自带 pnpm 下载依赖，TypeScript 和 Vite 构建通过。依赖锁文件保留供复现。
- 未验证：Unity Editor 导入、C# 编译、Android APK、AR 真机识别与 UI 防误触、手机到网页完整闭环。不得将这些项目标成通过；按 `docs/ACCEPTANCE.md` 实测后补充记录。
- 检查/修正过程 4：对照 Unity 官方 XR Origin 文档发现初稿把移动 AR 相机绑定到了 XRHMD；修正为 `<HandheldARInputDevice>/devicePosition` 与 `deviceRotation`，按手持布局处理跟踪状态；补充 `UnityEditor.Build` 命名空间和 Audio 模块依赖。该修正来自实际源码复查，仍需 Unity 编辑器验证。
- 浏览器检查：真实 Go API + Edge 无头浏览器验证了空列表、状态更新、进程重启、离线错误、重试恢复及失败时保留旧状态，未捕获 pageerror。通过 API 模拟手机提交，没有冒充 Unity 真机验证。

## P-002

- 目标：了解三端文件位置和启动方法。

### Prompt 原文

> 移动端，管理端，后端各是哪个文件，怎么运行

### 结果

- 采用（操作说明，待用户执行验证）。
- 给出 Unity、React、Go 入口文件和启动顺序；延用 P-001 已验证的本地命令，未新增真机验收结论。

## P-003

- 目标：确认 iOS 可行性及手机安装要求。

### Prompt 原文

> 移动端用IOS可以吗，需要在手机上下载安装哪些

### 结果

- 采用（平台说明，未执行 iOS 工程迁移）。
- 核对 Unity ARKit 6.0 文档及 Apple 官方真机运行、Developer Mode、Personal Team 文档，说明手机安装自研 App、Mac/Xcode 打包签名及权限要求。当前 Android 工程尚未改成 iOS，未声称 iOS 编译或真机测试通过。

## P-004

- 目标：根据只有 Windows 11 和 iPhone 的条件选择可行的真机开发路线。

### Prompt 原文

> 我没有Mac，只有window11系统，IOS 26.5.1

### 结果

- 采用（可行性说明，方案待用户选择）。
- 核对 Unity 官方 iOS 构建流程：Windows 可开发及导出 Xcode 工程，最终 iOS 编译需要 macOS/Xcode 或云构建；云构建仍需处理签名及分发。建议借用支持 ARCore 的 Android 真机完成现有原型，或借 Mac 测试 iPhone。未根据 iOS 版本推断手机型号或宣称兼容性已验证。

## P-005

- 目标：核实 iQOO Neo7 是否可用于 ARCore 真机测试。

### Prompt 原文

> 手机是IQOO Neo7 ,支持ARCore吗？

### 结果

- 采用（官方型号支持核验，尚未实测手机）。
- Google 官方 ARCore supported devices 页面中国市场列表包含 Vivo iQOO Neo7，标注支持 Depth API。该轮回复被用户中断，结论在 P-006 回复中补充；手机实际运行仍需测试。

## P-006

- 目标：在安卓手机到达前准备电脑环境及 Unity 工程。

### Prompt 原文

> 借到安卓手机了，还在路上，可以提前做哪些准备

### 结果

- 采用（准备步骤，尚未执行 Unity 安装或真机测试）。
- 检查常见 Unity Hub/Editor 安装路径未发现安装，给出 Hub、Unity 6.0 LTS 与 Android 模块准备、工程导入、场景生成和预构建步骤。没有把常见路径检查等同于全盘确认未安装；真机平面与防误触仍待手机到达后验证。

## P-007

- 目标：安装 Unity 与 Android 开发环境。

### Prompt 原文

> 现在帮我安装 Unity 环境

### 结果

- 未采用完整安装结果：安装被官方下载连接故障阻塞，尚未安装成功。
- 检查常见安装目录、卸载注册项与运行进程，没有发现 Unity Hub/Editor；D 盘约有 208 GB 可用，计划把编辑器装到 D 盘。
- 查询 Unity 官方 Hub CLI 和 Android 模块文档；官方旧 Hub 下载地址返回 404，改用新版官方 x64 地址。
- 新版 Hub 官方 CDN、官方更新清单、官方编辑器下载地址发生 DNS 或连接超时；Unity 发布 API 连接被远程关闭。Edge 导航下载地址也超时。
- 没有获得可校验的完整安装包，因此未执行安装、未声称已配置编辑器或 Android 工具链。恢复步骤与可重用脚本见 docs/UNITY_INSTALL.md、scripts/install-unity.ps1。

## P-008

- 目标：使用已下载的安装包继续安装 Unity 开发环境。

### Prompt 原文

> 已经下载在E:\yangjia\Downloads

### 结果

- 修改后采用（Hub 已安装；编辑器本轮安装超时失败，系统组件随后登记安装成功，待用户重启后恢复）。
- 找到 UnityHubSetup-x64.exe，验证 Authenticode 状态 Valid，签发主体为 Unity Technologies SF；静默安装退出码 0，核验 Hub 3.15.2.0 已安装。
- Hub 编辑器目录设置为 D:\Unity\Hub\Editor。通过有日志的 Hub headless 命令安装 6000.0.62f1 和 Android 支持；停止了同一任务产生的旧重复安装实例，保留当前安装进程。
- Android Build Support 安装包已下载，编辑器正在下载；尚未声称编辑器或完整工具链安装成功。
- 后续编辑器下载完成并解压主要文件；安装器持续等待 DISM 子进程。对照 DISM/CBS 日志，确认 Windows 正在在线启用 NetFx3（.NET Framework 3.5），下载进度多次停留约 49%。保留后台安装，不强杀系统服务。
- 已排队在编辑器安装任务退出后补装 Android 全部子模块；该步骤还未开始，不能声称 SDK/NDK/JDK 已齐全。当前状态与日志路径更新至 docs/UNITY_INSTALL.md。
- 后续 Hub 安装超过 30 分钟失败，Android 子模块命令也因编辑器未登记而返回 No modules found for this editor。系统组件下载继续进行并达到 100%；最新核验 NetFx3 Install=1，但系统 CBSRebootPending 仍存在。要求用户正常重启后再恢复安装，未自动重启或强杀系统服务。
- 将已下载的编辑器安装包保存至 D:\Unity\Installers\UnitySetup64-6000.0.62f1.exe，核对大小一致，便于恢复。尚未完成 Android 工具链验收、Unity 编译或 APK 打包。

## P-009

- 目标：重启 Windows 后继续安装 Unity 编辑器和 Android 开发环境。

### Prompt 原文

> 继续安装 Unity

### 结果

- 修改后采用（编辑器与 JDK 已安装；Android 支持和 SDK/NDK 尚未完成）。
- 实测 NetFx3 Install=1，CBS RebootPending 已清除。使用 D 盘已保存且此前已验证签名的官方安装包，静默恢复相同版本、相同目录的编辑器安装。
- Hub 能列出 6000.0.62f1 的编辑器路径；该识别结果不等于模块齐全或项目已编译。
- 编辑器官方安装器退出码 0；实际运行 Unity.exe -version 返回 6000.0.62f1、退出码 0。OpenJDK 官方压缩包续传完成、安装后 java -version 返回 17.0.9。
- 检查并放弃了重复 Hub 安装方案：install-modules 因模块登记缺失失败，完整 install 则因路径已有编辑器失败；没有伪造 modules.json。改用官方独立 Android 支持安装包，数字签名 Valid、MD5 与官方缓存清单一致，但 Windows 管理员授权返回“操作已被用户取消”，未宣称模块装好。
- 检查 AI 编写的并行下载辅助脚本时，实际运行发现 ForEach-Object -Parallel 不支持显式 ErrorAction 公共参数，移除该参数并增加完整下载标记核验。修正后 Google 官方域名解析失败；官方中国域名及官方重定向域名也超时，未使用来源不明镜像或改动系统网络设置。
- 发起 Unity 工程批量导入，尚未获得 C# 编译或 APK 构建成功结果。已更新安装状态和验证文档，保留真机验收未完成的边界。
- 批量导入停留在 AssetDatabase/Package Manager 初始化阶段，伴随许可证 entitlement 错误；没有成功退出或编译结果。核对命令行后只停止本轮专用 Unity 批处理进程，并打开 Hub 交给用户手动处理账号和许可证。

## P-010

- 目标：运行本地 Android 支持安装包。

### Prompt 原文

> **Unity Hub**账号已经登录完成，现在要怎么运行本地的Android支持安装包

### 结果

- 采用：说明本地安装包、管理员授权和目标编辑器目录；尚未据此声称安装成功。

## P-011

- 目标：辨认 Hub 截图中的错误。

### Prompt 原文

> 安装什么东西失败了

### 结果

- 采用：根据截图区分许可证激活失败和 Unity 6.6 安装失败，提醒项目基线是 6000.0.62f1。

## P-012

- 目标：取得并激活 Unity Personal 许可证。

### Prompt 原文

> 我只注册了Unity的账号，许可证要去哪里获得

### 结果

- 采用：查询 Unity 官方许可证文档，说明 Licenses → Add license → Get a free personal license 路径，由用户本人激活。

## P-013

- 目标：确认登录并切换 Hub 界面语言。

### Prompt 原文

> 这样是不是登陆完成了，能不能把语言切换为中文

### 结果

- 采用：读取 Hub 实际界面，许可证页显示 Personal 和激活日期；设置 → 外观 → 语言选择简体中文，重新观察确认中文界面已生效。

## P-014

- 目标：明确剩余工作顺序。

### Prompt 原文

> 后面的任务是什么

### 结果

- 采用：明确先补齐 Android 工具链，再验证 Unity 编译、构建 APK、真机 AR 及三端联调。

## P-015

- 目标：选择所需安装组件，并继续安装 Unity 6000.0.62f1。

### Prompt 原文

> 现在提示我没有安装6000.0.62f1，请帮我选择安装所需的平台、语言包、文档等信息，并继续安装

### 结果

- 采用（安装任务已启动，尚未验收完成）。
- 先核验 D 盘编辑器文件仍存在，再读取 Hub 当前实际安装向导。取消非必需 Visual Studio，选择 Android Build Support 及自动勾选的 OpenJDK、Android SDK & NDK Tools；保留简体中文语言包，其他平台及离线 Documentation 不选。
- 按用户明确安装指令继续向导并启动任务；展开下载面板，实际显示编辑器、简体中文与 Android 子组件共 13 项，初始完成 0 项。下载/排队不等于安装成功。
- 后续实际观察到编辑器与 Android SDK Command Line Tools 均下载到 2%，速度约 3.13 MB/s 和 2.47 MB/s，说明本轮下载已推进；简体中文等待安装，仍需完成后核验。

## P-016

- 目标：排查打开 Unity 工程时的依赖下载错误。

### Prompt 原文

> 有报错

### 结果

- 修改后采用（正在验证依赖解析）。
- 根据用户截图识别 packages.unity.com 的 ENOTFOUND 域名解析错误，涉及 Input System 和 ARCore。核验项目依赖仍为指定版本；实际 DNS 解析及 Unity 官方国际、中国区包服务 HEAD 请求均成功（HTTP 200）。
- 错误窗口随后关闭，实际 Unity 窗口进入 Resolving packages。没有将 UI 点击失败冒充重试成功；继续检查真实编辑器日志和包缓存。
- 当前运行的是 C:\Program Files\Unity\Hub\Editor\6000.0.62f1\Editor\Unity.exe；SDK adb、NDK source.properties、JDK java 文件已存在，NDK Revision=27.2.12479018。该文件检查不能替代 APK 构建和真机验收。
- 初始国际源重试仍停留 Resolving packages；中国区元数据请求首次超时，改用 curl 实测两个指定版本均存在，Input System 下载端点返回官方 CDN 重定向。按 Unity 官方 manifest 的 registry 配置，将项目级包源切换为 https://packages.unity.cn，不改系统 DNS、代理或依赖版本。
- 随后在 Library/PackageCache 实际看到原先缺失的 com.unity.inputsystem 与 com.unity.xr.arcore，Editor.log 已进入脚本编译；不把时间先后直接当作唯一根因证据，最终编译结果另行核验。
- 最终 Editor.log 显示 Tundra build success（23.04 seconds），Assembly-CSharp.dll、Assembly-CSharp-Editor.dll 已生成，C# 编译通过。已更新 VALIDATION.md，仍不将此当作场景、APK 或 AR 真机验收通过。

## P-017

- 目标：解释输入系统启用提示。

### Prompt 原文

> 这个警告是什么原因

### 结果

- 修改后采用：最初建议 Both；后续真实 Android 构建显示不支持 Both，因此放弃该建议，将触摸、UI 和相机姿态统一到新版 Input System，修正生成器及 README。

## P-018

- 目标：准备安卓手机运行。

### Prompt 原文

> 打开了，接下来要怎么做，我的安卓手机也已经到了

### 结果

- 修改后采用：指导生成场景、切 Android、启用 ARCore、连接 USB；其中 Both 的指导在下一轮实际构建时被检查并纠正。

## P-019

- 目标：检查已完成配置并继续构建手机 APK。

### Prompt 原文

> 四个步骤均已完成

### 结果

- 修改后采用（APK 构建验证中）。
- ADB 实测设备状态 device，手机 Android 13，Google AR 服务包存在；不记录设备序列号。核验 Inspection 场景、Android ARCore loader、ARM64 和 IL2CPP 配置。
- Go 可执行文件实际位于 .tools，纠正首次误写 backend 路径后，以 backend 为工作目录启动，/health 返回 ok，正式数据文件为 backend/data/issues.json；建立 adb reverse tcp:8080 tcp:8080。
- 首次点击 Build，真实弹窗报告 Both 不受 Android 支持，选择 No 取消，未忽略警告。将旧 Input.GetTouch 改为 Touchscreen，保留显式 UI 射线防误放；EventSystem 改 InputSystemUIInputModule 并分配默认 actions。
- 增加 Inspection/Build USB Android APK 菜单，更新已有场景输入模块和 USB 后端地址 http://127.0.0.1:8080，保存后生成 Builds/inspection.apk；实际构建结果待继续核验。
- 修正后的脚本实际重编译通过；重新执行 USB 构建菜单，ARM64 IL2CPP 和 Gradle 打包完成，Editor.log 输出 APK_BUILD_SUCCEEDED，产物 25,275,219 字节，SHA256 已记录到 VALIDATION.md。
- ADB 安装失败，手机返回 INSTALL_FAILED_ABORTED: User rejected permissions。未冒充已安装、未绕过手机安装限制或授予相机权限；等待用户本人处理手机安装确认后再继续。Go 后端保留运行以供后续联调。

## P-020

- 目标：说明手机所需应用与安装方式。

### Prompt 原文

> 需要在手机上安装什么，怎么安装

### 结果

- 采用：说明已检查 Google AR 服务存在，需要安装本项目 Park Inspection APK；提供 USB 安装和手动复制 APK 的步骤，未要求手机安装编辑器。

## P-021

- 目标：用户确认后重新安装手机应用。

### Prompt 原文

> 可以安装了

### 结果

- 采用（正在核验安装结果）。
- 重新确认 ADB 状态 device，发起 inspection.apk 安装；仅在返回成功后才启动 APK 中实际声明的 UnityPlayerGameActivity，并恢复 USB 8080 转发。不通过 ADB 绕过用户相机授权。
- 本轮安装请求持续等待，另行通过 pm path 核对尚无本应用安装路径；因此未宣称安装或启动成功。保留当前安装请求，提示用户检查手机安装确认。

## P-022

- 目标：了解手机原型使用流程。

### Prompt 原文

> 已经安装好了，在手机上要怎么使用？

### 结果

- 采用：说明 USB 后端地址、相机权限、扫描水平面、放球、填表上报和网页核对；安装完成由用户报告，未将操作说明当作验收结果。

## P-023

- 目标：解释检测平面和问题标记的作用。

### Prompt 原文

> 成功了，绿色的平面和紫色球是做什么的

### 结果

- 采用：解释平面为检测结果、球为问题位置，坐标属于本次 AR 会话；用户报告看到绿色平面和紫色球，未据此断言上报链路已通过。设计色为橙色，将紫/粉显示列为待检查项。

## P-024

- 目标：检查球形标记颜色显示异常。

### Prompt 原文

> 怎么检查显示问题

### 结果

- 采用：实际检查源码，球通过 CreatePrimitive 运行时生成，只修改默认材质颜色，没有独立标记材质资源；GraphicsSettings 使用 Built-in 管线。只读取本应用进程最近日志，未捕获 Shader/材质错误，因此没有把疑似 Shader 缺失当作已证实原因。
- 查询 Unity 官方错误 Shader 与构建 Shader 引用文档，说明应结合实际球颜色、目标机日志与显式材质对照判断；当前尚未验证颜色修复。

## P-025

- 目标：编写项目 README。

### Prompt 原文

> 请根据这个项目帮我写一个`README.md`，说明：使用的版本；三个部分如何启动；Unity 真机如何配置 Go 后端地址；已完成、未完成和已知问题；

### 结果

- 修改后采用：核对配置、锁文件和实际版本，重写 README 的三端启动、USB/局域网地址、API 及完成范围；保留紫色球待排查和完整真机闭环待验收，不把用户反馈等同于全部验收通过。

## P-026

- 目标：将移动端提交结果改为弹窗反馈。

### Prompt 原文

> 现在移动端提交后，提示信息显示在下方，改为弹窗形式

### 结果

- 修改后采用（编译通过，交互及 APK 验证中）。
- 在 InspectionController 增加全屏遮罩和居中弹窗，提交中不可关闭，成功显示 ID，失败或参数错误显示原因；结果点 OK 关闭。长信息可滚动，弹窗期间禁止背景 AR 放置和重复提交，失败仍保留输入和标记。
- 原有扫描和放置坐标提示保留在底部。实际 Unity 日志显示修改后 C# 编译通过；继续进行编辑器布局和新版 APK 验证。
- 编辑器实际 Play 模式验证：空标题提交显示 Cannot submit / Title is required 弹窗；点击遮罩范围内的背景 Priority 按钮后，弹窗保持且优先级仍为 medium；点 OK 关闭后恢复表单。上述验证不替代手机上报成功/失败及 AR 触摸验收。
- 新版 APK 实际构建成功，大小 25,277,467 字节。覆盖安装时手机返回 INSTALL_FAILED_ABORTED: User rejected permissions，因此未声称手机已更新；保留产物供用户确认安装后更新。后端 /health 实测返回 ok。

## P-027

- 目标：继续覆盖安装提交弹窗版本。

### Prompt 原文

> 继续

### 结果

- 采用（正在核验手机安装结果）。
- ADB 状态为 device，重新发起新版 APK 覆盖安装；安装成功后才恢复 USB 8080 转发并启动应用，不绕过手机确认。
- 实际返回安装 Success，USB 8080 转发恢复成功，启动 UnityPlayerGameActivity，后端 /health 返回 ok。手机应用进程已运行；成功/失败弹窗与真实 AR 上报的现场操作仍需实际验证。

## P-028

- 目标：恢复移动端与后端连接，排查 8080 占用。

### Prompt 原文

> 移动端突然连接不上后端了

### 结果

- 修改后采用。
- 核对截图为第二个 Go 进程启动时端口占用；实测占用 8080 的是本项目 .tools/inspection.exe，/health 返回 ok，未停止已有后端或启动重复进程。
- 手机状态 device，但 reverse 列表缺少 8080；恢复 adb reverse tcp:8080 tcp:8080 并实际核验规则已存在。手机 nc 健康探针未取得响应，不把它当作手机 HTTP 请求已通过的证据。
- 增加 scripts/connect-android.ps1，实跑通过设备授权、电脑后端健康检查和端口转发恢复，README 增补使用说明。未修改系统网络安全设置或清除应用数据，移动端提交需用户重试确认。

## P-029

- 目标：定位移动端再次无法连接后端的原因。

### Prompt 原文

> 又连接不上后端了是什么原因

### 结果

- 采用。
- 实测电脑 Go /health 返回 ok，而 ADB get-state 返回 device offline，说明当前故障位于 USB 调试链路，不能解释为 Go 已停止。
- 执行 adb reconnect offline 尝试恢复，随后状态变为 unauthorized；需要用户本人在手机确认 USB 调试授权。未按工具提示擅自清除密钥或重启全局 ADB 服务，未将软件重连当作已恢复连接。
- 提示用户解锁并允许调试，再执行现有 connect-android.ps1 重建 8080 转发；反复掉线的物理或系统根因尚不能从此日志确定，局域网连接可减少对 USB 转发的依赖。

## P-030

- 目标：为两段演示视频添加说明并剪辑为一段。

### Prompt 原文

> 这两个是我拍摄的演示视频，第一个视频是手机端和React，第二个视频是Go后端，演示重启后数据仍然存在。请帮我在视频里添加说明，并把两个视频剪辑到一起

### 结果

- 修改后采用。
- 实际读取两段源视频：手机竖屏 720×1600、约 74.71 秒，后端横屏 2550×1528、约 27.97 秒；逐段抽帧查看扫描、标记、填写、成功弹窗、React 更新、后端停止/报错/重启及原记录。
- 编写 scripts/edit-demo-video.py，用 Pillow 绘制中文说明面板，FFmpeg 合成为 1080p 横屏合辑；手机完整画面配局部放大，关键成功回执定格，等待片段加速并标注倍率，保留源音轨，无额外配音。只读取原视频，没有覆盖原文件。
- 先渲染并目视检查成功回执样片，再渲染完整 93.02 秒 MP4；抽查关键段落排版，修订依据始终为实际画面，未将紫色球改色或虚构数据验证结果。
- 最终文件完整解码返回 0、无解码错误，H.264 1920×1080/30fps、AAC 48kHz 立体声，约 13.45 MiB；同时交付可编辑 SRT、时间线 JSON、封面和说明。


## P-031

- 目标：仅发布项目相关文件到用户指定的 GitHub 与 Gitee 仓库。

### Prompt 原文

> 现在将该项目推送到github和gitee上，无关的文件不要上传。[https://github.com/Jiang-JiangYa/Unity-AR--.git](https://github.com/Jiang-JiangYa/Unity-AR--.git)   [https://gitee.com/Jiang-Jiang-ya/unity-ar-park-area-inspection.git](https://gitee.com/Jiang-Jiang-ya/unity-ar-park-area-inspection.git)

### 结果

- 修改后采用（发布准备已验证，远程推送结果随后补记）。
- 两个指定远程仓库 ls-remote 均成功且无现有分支；本地 master 尚无提交，不覆盖远程历史。
- 补齐 Unity .utmp、编辑器本地偏好、缓存、环境文件和签名密钥的忽略规则。仅收录三端源码、Unity 必需资源和设置、锁文件、项目文档、相关辅助脚本及根目录演示视频；排除 .tools、node_modules、Library、APK、JSON 运行数据和本地 .env。
- 待发布 81 个文件约 13.8 MiB，常见敏感凭据格式扫描未发现匹配；当前 Go test、go vet、React TypeScript/Vite 构建均通过。未恢复用户已删除的 AI 日志部分。
