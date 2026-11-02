# 园区 AR 巡检原型

巡检人员用安卓手机检测真实水平面、放置问题标记并上报；Go 后端将问题持久化到本地 JSON；管理人员用 React 网页查看问题、修改处理状态。

流程：**Unity AR 放置标记并上报 → Go 保存 → React 查看 → React 修改状态**。三端使用同一个 Go 服务和同一份数据，不需要接入大模型。

## 1. 项目目录

| 部分 | 目录 / 主要文件 | 用途 |
| --- | --- | --- |
| 移动端 | `unity/Assets/Scripts/InspectionController.cs` | AR 点击放置、表单、HTTP 上报、反馈及 UI 防误放 |
| 场景生成 | `unity/Assets/Editor/InspectionSetup.cs` | `Inspection → Create Android AR Scene` 菜单 |
| USB 构建 | `unity/Assets/Editor/InspectionBuild.cs` | `Inspection → Build USB Android APK` 菜单 |
| AR 场景 | `unity/Assets/Scenes/Inspection.unity` | 真机构建场景 |
| 后端 | `backend/main.go`、`backend/main_test.go` | HTTP API、参数校验、JSON 持久化与测试 |
| 管理端 | `admin/src/main.tsx`、`admin/src/style.css` | 问题列表、筛选、状态更新及错误提示 |
| 使用与验证 | `docs/PHONE_TEST.md`、`docs/VALIDATION.md`、`docs/ACCEPTANCE.md` | 手机操作、验证证据与验收清单 |
| AI 记录 | `AI_LOG.md` | 实际 Prompt 原文、采用情况和纠错记录 |
| 演示视频 | `园区AR巡检_演示合辑.mp4`（项目根目录） | 中文说明版：手机上报、React 状态更新、Go 停止与重启验证 |

`unity/Builds/`、`unity/Library/`、`admin/node_modules/`、`backend/data/`、`.tools/` 均被 Git 忽略。APK、运行数据和本机便携工具不作为源码依赖提交。

## 2. 使用的版本

以下为项目配置、锁文件及本机已核验的版本，不代表使用这些工具的最新版本。

| 项目 | 版本 / 配置 |
| --- | --- |
| 开发电脑 | Windows 11、PowerShell |
| Unity Editor | Unity 6.0 LTS：`6000.0.62f1`，revision `f99f05b3e950` |
| 渲染管线 | Built-in Render Pipeline |
| AR Foundation / ARCore XR Plugin | `6.0.8` / `6.0.8` |
| Input System / XR Plug-in Management | `1.11.2` / `4.5.0` |
| XR Core Utilities / Unity UI | `2.4.0` / `2.0.0` |
| Android 构建 | ARM64、IL2CPP、OpenGLES3；最低 API 24 |
| Android 工具链 | OpenJDK `17.0.9`、NDK r27c（`27.2.12479018`）、SDK Build Tools `36.0.0` |
| 测试手机 | iQOO Neo7，ADB 型号 `V2232A`，Android 13；已检测到 Google Play Services for AR |
| Go | `go.mod` 要求 `1.22+`；本机已验证 `1.27.3`，使用标准库，无第三方框架 |
| Node.js | 本机 `24.21.0`；Vite 依赖要求 `20.19+` 或 `22.12+`，建议 Node 22.12+ |
| React / React DOM | 锁文件实际解析版本均为 `19.3.1` |
| TypeScript / Vite | `5.9.3` / `7.3.7` |
| 包管理器 | 已验证 pnpm `11.25.0`；按 `admin/pnpm-lock.yaml` 冻结安装 |

Unity 版本见 `unity/ProjectSettings/ProjectVersion.txt`；包版本见 `unity/Packages/manifest.json` 和 `packages-lock.json`。前端 `package.json` 中存在版本范围，以 `pnpm-lock.yaml` 中锁定的版本为准。升级 Unity 或依赖后需重新验证构建和真机行为。

## 3. 启动 Go 后端

以下命令以仓库根目录为起点。后端、管理端分别在两个 PowerShell 终端运行。

```powershell
cd .\backend
go test -v ./...
go vet ./...
go run .
```

本机若没有将 Go 加入 PATH，可在 `backend` 目录使用 `..\.tools\go\bin\go.exe run .`。`.tools` 是本机辅助目录，新电脑应自行安装 Go。

检查：浏览器打开 `http://localhost:8080/health`，应返回：

```json
{"status":"ok"}
```

默认监听 `:8080`，数据保存到**进程工作目录**下的 `data/issues.json`。从 `backend` 启动时就是 `backend/data/issues.json`，第一次成功写入时创建。重启时保持相同工作目录，或显式指定绝对数据路径：

```powershell
# 在 backend 目录执行，再启动服务
$env:ADDR = ':8080'
$env:DATA_FILE = Join-Path (Get-Location).Path 'data\issues.json'
$env:CORS_ORIGINS = 'http://localhost:5173,http://127.0.0.1:5173'
go run .
```

| 环境变量 | 默认值 | 说明 |
| --- | --- | --- |
| `ADDR` | `:8080` | HTTP 监听地址 |
| `DATA_FILE` | `data/issues.json` | JSON 数据文件，推荐使用绝对路径 |
| `CORS_ORIGINS` | `http://localhost:5173,http://127.0.0.1:5173` | 允许的浏览器来源，多个来源用逗号分隔 |

同一数据文件只由一个 Go 进程写入。`/health` 只确认 HTTP 服务响应，不代表磁盘可写或三端流程已验收。

### 停止与重启演示

在运行 `go run .` 的终端按 **Ctrl+C** 停止服务；管理页点击“刷新列表”，应显示连接错误。再从同一个 `backend` 工作目录执行 `go run .`（本机也可用 `..\.tools\go\bin\go.exe run .`），网页点击“重试”，核对原记录和处理状态仍然存在。停止服务不会删除 JSON 文件。若启动时提示 8080 已占用，先检查是否已有后端运行，不要同时启动两个实例。

## 4. 启动 React 管理端

在另一个终端中，从仓库根目录执行：

```powershell
cd .\admin
pnpm install --frozen-lockfile
pnpm dev --port 5173 --strictPort
```

浏览器访问 `http://localhost:5173/`。默认 API 地址是 `http://localhost:8080`。

需要更换后端时，在 `admin` 目录执行：

```powershell
Copy-Item .env.example .env.local
```

修改 `.env.local`：

```dotenv
VITE_API_BASE_URL=http://localhost:8080
```

修改后重启 Vite。生产构建命令为 `pnpm build`；生产包中的 API 地址在构建时写入，更改后需重新构建。不要把 `/api` 附加到上述基础地址，页面代码会自行拼接接口路径。

管理端显示标题、描述、优先级、状态、坐标和时间，可筛选状态，并在 `open`、`in_progress`、`resolved` 之间切换。页面不自动轮询，手机提交后点击“刷新列表”；后端不可用时显示错误与“重试”，不会直接白屏。

## 5. 配置并运行 Unity 移动端

### 5.1 导入和构建

1. 在 Unity Hub 登录并激活有效许可证；安装 `6000.0.62f1` 及 **Android Build Support、Android SDK & NDK Tools、OpenJDK**。手机不需要安装 Hub 或编辑器。
2. 在 Hub 中添加本仓库的 `unity` 目录，等待依赖导入。工程当前使用 Unity 官方中国区包源 `https://packages.unity.cn`。
3. 已有场景可直接打开 `Assets/Scenes/Inspection.unity`。需要重新生成时使用 **Inspection → Create Android AR Scene**；它会重建场景，应先保存自己的修改。
4. 在 **File → Build Profiles** 选择 Android 并切换平台；确认 Scene List 包含且启用 `Inspection`。
5. 在 **Edit → Project Settings → XR Plug-in Management → Android** 勾选 ARCore，开启 Initialize XR on Startup；ARCore 使用 Required，处理阻止构建的 Project Validation 项。
6. 在 **Player → Other Settings** 确认：Active Input Handling 为 **Input System Package (New)**、最低 API 24、IL2CPP、ARM64、Graphics APIs 为 OpenGLES3、Internet Access 为 Require、Allow downloads over HTTP 为 Always Allowed。如要求重启则重启。Android 不使用 Both；触摸、UI 与 AR 相机姿态已统一使用新版输入系统。
7. 根据下面的 USB 或局域网方式配置后端，构建 APK。手机开启开发者选项、USB 调试，连接电脑并本人确认调试和安装授权。
8. 打开手机上的 **Park Inspection**，允许相机权限。需要支持 ARCore 的真机及 Google Play Services for AR；Editor Play 无法替代真实平面检测验收。

### 5.2 方式 A：USB 联调

手机通过 USB 访问电脑的同一个 Go 后端，无需手机与电脑处于同一 Wi-Fi。

1. 启动第 3 节的 Go 服务。
2. 在 Unity 执行 **Inspection → Build USB Android APK**。该菜单会保存场景、配置新版 UI 输入模块，并将场景的后端地址设为 `http://127.0.0.1:8080`，输出 `unity/Builds/inspection.apk`。
3. 在仓库根目录执行以下命令。`$adb` 路径需对应自己实际安装的 Unity SDK：

```powershell
$adb = 'C:\Program Files\Unity\Hub\Editor\6000.0.62f1\Editor\Data\PlaybackEngines\AndroidPlayer\SDK\platform-tools\adb.exe'
& $adb devices
& $adb reverse tcp:8080 tcp:8080
& $adb install -r .\unity\Builds\inspection.apk
```

设备应显示 `device`；`unauthorized` 表示需要在手机确认 USB 调试。手机若弹出安装提示，需要本人确认。打开 Park Inspection，表单第一栏 **Backend URL** 保持：

```text
http://127.0.0.1:8080
```

这里的回环地址能访问电脑，是因为 `adb reverse` 建立了转发。测试期间保持 USB 连接；断开重连、手机或 ADB 重启后重新执行转发命令。Go 端口改变时，转发端口和手机地址也需同步调整。

USB 转发丢失时，可在仓库根目录执行 `.\scripts\connect-android.ps1`，检查电脑后端及手机授权后恢复 8080 转发；Unity 安装位置不同时通过 `-AdbPath` 指定实际 adb.exe。脚本不启动或结束后端进程。若启动 Go 提示 8080 已占用，先访问 `/health`；原服务正常时直接使用它，不要重复启动。

### 5.3 方式 B：同一局域网联调

1. 手机和电脑接入可相互访问的同一局域网，电脑用 `ipconfig` 查询当前网卡 IPv4。例如电脑地址为 `192.168.1.100`。
2. Go 使用 `ADDR=:8080` 或对应网卡地址监听；必要时仅为可信私有网络允许 TCP 8080 入站，避免访客 Wi-Fi 的设备隔离。
3. 先在手机浏览器访问 `http://192.168.1.100:8080/health`，确认返回 `{"status":"ok"}`。
4. 在手机表单第一栏 **Backend URL** 填写 `http://192.168.1.100:8080`，或在 Unity 场景 **XR Origin → InspectionController → Api Base Url** 中修改并保存后用普通 Build / Build And Run 构建。

局域网模式不能把电脑后端写成 `localhost`、`127.0.0.1` 或 `0.0.0.0`：前两者指向手机自身，后者是监听地址。USB 构建菜单会将场景地址重新设为回环地址，局域网测试应在运行时改回电脑 IP，或使用普通构建。

演示视频中的 `172.20.10.5:8080` 是录制当时的电脑地址，仅供识别画面；换网络或重连热点后可能变化，请重新查询实际 IPv4。使用局域网地址运行已安装的应用不依赖数据线，USB 主要用于安装、调试或上述端口转发模式。

Unity 原生 HTTP 请求不受浏览器 CORS 限制。电脑网页仍使用第 4 节的 API 地址；若通过 `http://电脑IP:5173` 访问网页，需将该准确来源加入 Go 的 `CORS_ORIGINS`，同时将前端 API 基础地址改为电脑 IP。修改 Unity 地址不会自动修改 React 配置。

### 5.4 手机使用

缓慢扫描有纹理、光照充足的水平桌面或地面，等待绿色平面和 `SessionTracking`，点击表单以外的平面放置标记，再填写标题、可选描述、优先级并点 **Submit issue**。点击新位置会移动当前标记。

提交中显示等待弹窗；成功后弹窗显示 `Submitted successfully` 和后端 ID，点 OK 关闭。失败或参数错误也使用弹窗，失败保留表单和标记。弹窗期间拦截背景点击及 AR 放置；扫描和放置坐标提示仍显示在底部。回到网页点击“刷新列表”，核对同一 ID 并修改状态。详细步骤见 `docs/PHONE_TEST.md`。

`position` 为本次 AR 会话的世界坐标，单位为米，不是 GPS 或园区统一地图坐标。球体中心向上偏移 6 cm 便于观察，上报仍使用平面命中坐标。应用重启后不恢复历史标记的真实物理位置。

## 6. API 与基础数据结构

成功直接返回对象或数组；业务错误返回 `{"error":"错误信息"}`。请求为 UTF-8 JSON。

| 方法 | 路径 | 请求 / 成功响应 |
| --- | --- | --- |
| GET | `/health` | 200，`{"status":"ok"}` |
| POST | `/api/issues` | 提交 title、description、priority、position；201，完整问题对象 |
| GET | `/api/issues` | 200，问题数组，空列表为 `[]` |
| PATCH | `/api/issues/{id}/status` | 请求 `{"status":"in_progress"}`；200，更新后的问题对象 |

创建请求：

```json
{
  "title": "入口墙面破损",
  "description": "左侧墙体存在裂缝",
  "priority": "high",
  "position": { "x": 0.42, "y": 0.03, "z": 1.26 }
}
```

三端字段一致：`id`、`title`、`description`、`priority`、`status`、`position.x/y/z`、`createdAt`、`updatedAt`。后端生成随机 128 位十六进制 ID，默认状态 `open`，时间为 UTC RFC3339；修改状态保留 createdAt，更新 updatedAt。

标题去除首尾空白后不能为空；priority 为 `low / medium / high`；status 为 `open / in_progress / resolved`；坐标为必填数字，允许 0 和负数，不允许缺省或 null。请求体上限 64 KiB，不接受未知字段或多个 JSON 对象。参数错误返回 400，ID 不存在返回 404，持久化失败返回 500。

写入使用进程内互斥锁、同目录临时文件、同步与重命名；落盘失败不发布内存变更。损坏 JSON 会阻止启动并保留原文件。数据可在服务重启后读取，但不提供数据库级多进程事务能力。

## 7. 已完成与待完成

状态截至 2026-11-03。区分代码实现、工具验证、用户反馈和录屏证据；录屏展示的主流程不代表全部边界场景都已验收。

| 部分 | 已完成 / 已有证据 | 尚未完成的验收 |
| --- | --- | --- |
| Go | 四类 API、ID/时间生成、默认状态、参数校验、JSON 持久化、CORS 已实现；6 组测试、go vet、构建与真实进程重启测试通过；录屏展示停止、原目录重启后原记录和状态保留 | 其他边界场景继续按验收清单核对 |
| React | 列表、状态修改、筛选、错误提示已实现；TypeScript / Vite 构建及浏览器联调通过；录屏展示手机问题进入列表、改为已解决、服务停止时报错、重启后恢复 | 全部状态切换及异常场景的真机配合测试仍需补齐 |
| Unity | 水平面检测、单个当前标记、表单上报、显式 UI 射线防误放已实现；编译及 ARM64 IL2CPP APK 构建通过；提交等待、结果及参数错误已改为弹窗，编辑器验证了关闭和背景遮罩行为 | UI 防误放、三种优先级、失败保留、跟踪丢失等真机逐项验收 |
| 手机运行 | 新版 APK 已覆盖安装并启动；录屏展示真实平面检测、放置标记、输入中文标题和 low 优先级、提交成功弹窗、网页显示并更新对应记录 | 紫色球显示问题修复与复测；尚未展示的失败与权限路径仍待验收 |
| 文档与视频 | README、AI_LOG、手机测试步骤、验收清单及根目录演示合辑已提供 | 将后续实际执行的边界测试结果补入验收清单 |

已有工具验证详见 `docs/VALIDATION.md`；完整清单见 `docs/ACCEPTANCE.md`。后续录屏中可核对“窗台脏污”问题的短 ID `19e561b8`、low 优先级、坐标及已解决状态：手机返回成功回执，React 显示对应记录，Go 重启后该记录及状态仍保留。早期验证文档中的待测项反映当时进度，新增主流程证据见根目录视频；未展示项目仍需实际执行。

## 8. 已知问题与限制

- **球形标记显示紫色，尚未修复。** 设计颜色为橙色；当前运行时创建球体并修改默认材质，没有独立标记材质资源。材质或 Shader 缺失/不兼容是排查方向，最近本应用日志未捕获相关错误，根因尚未证实。需用显式材质引用重新构建并在手机对照验证。
- **包下载可能因网络失败。** 曾出现 `packages.unity.com` 的 ENOTFOUND，现项目配置官方中国区包源。出现错误时先检查相应服务连通性，再重试，不能把下载失败当作代码编译错误。
- **Both 输入模式已纠正。** 首次 Android 构建提示不支持 Both，现统一使用新版 Input System。旧操作记录中的 Both 指导不再适用。
- **USB 安装与相机权限需手机本人确认。** 曾出现 `INSTALL_FAILED_ABORTED: User rejected permissions`；之后用户已确认安装完成。设备显示 unauthorized 时也需本人确认调试授权。
- **手机端 UI 主要为英文。** 未内置专用中文字库；本次手机录屏已展示中文标题“窗台脏污”的输入和显示，其他设备及更广字符范围的字体覆盖仍未验证。
- **没有自动轮询、幂等键或自动重试。** 手机提交后网页需手动刷新；超时可能发生在后端已保存之后，应先核对网页再重试，以免产生重复问题。
- **定位范围有限。** 只检测水平面；不提供垂直墙面锚定、跨会话/跨设备空间定位、历史标记恢复或地图浏览。这些不属于当前要求的实现范围。
- **原型使用边界。** 使用本地 JSON、单进程写入；未提供登录鉴权、图片上传和删除功能。HTTP 仅用于可信局域网或 USB 演示，不作为公网部署方案。

安装过程的历史记录见 `docs/UNITY_INSTALL.md`，其中早期未完成状态仅描述当时情况；当前完成范围以上面的状态表和实际验证记录为准。

## 9. 演示视频

成片保存在项目根目录：[园区AR巡检_演示合辑.mp4](./园区AR巡检_演示合辑.mp4)。时长约 **1 分 33 秒**，1920 × 1080、30 fps，H.264 视频与 AAC 音频。中文步骤说明已合成进画面，保留源视频声音，播放时无需额外字幕文件。

| 合辑时间（约） | 演示内容 |
| --- | --- |
| 00:00–00:03 | 项目和流程片头 |
| 00:03–00:21 | 手机扫描水平面、点击放置标记 |
| 00:21–00:36 | 填写问题、选择优先级、提交成功弹窗和 ID 定格 |
| 00:36–01:02 | React 刷新列表并将对应问题改为已解决 |
| 01:02–01:15 | 查看原记录，在终端停止 Go 服务 |
| 01:15–01:21 | 后端停止后网页显示连接错误 |
| 01:21–01:30 | 从相同目录重启 Go，恢复读取原记录和状态 |
| 01:30–01:33 | 流程片尾 |
