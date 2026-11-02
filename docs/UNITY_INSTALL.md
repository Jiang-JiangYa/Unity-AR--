# Unity 环境安装状态

2026-10-31 继续安装：用户在 Hub 中发起 6000.0.62f1 安装向导，已选择 Android Build Support、OpenJDK、SDK & NDK Tools 及简体中文语言包，取消 Visual Studio，不选其他平台及离线文档。已启动共 13 项安装任务；最近观察到编辑器下载 2%（约 3.13 MB/s）、Command Line Tools 下载 2%（约 2.47 MB/s），简体中文等待安装。此为进度记录，不能视为工具链已装齐。安装完成后必须重新核验实际编辑器路径、adb、NDK、JDK 与项目编译。

2026-10-29 重启后更新：Unity Hub 3.15.2.0、Unity 6000.0.62f1 编辑器、OpenJDK 17.0.9 已安装。NetFx3 Install=1，CBS RebootPending=False。编辑器恢复安装退出码 0，运行 `Unity.exe -version` 返回 6000.0.62f1、退出码 0；`java.exe -version` 返回 17.0.9。

Android 环境仍未齐全：Android Build Support 官方安装包已完整下载并校验，启动安装时 Windows 返回“操作已被用户取消”（UAC 授权需要用户本人处理）。SDK/NDK 下载因 `dl.google.com` DNS 失败而中断；Google 官方中国域名和官方重定向域名也解析超时。尚未验证 C# 编译或 APK 构建。

本轮工程批量导入停留在 AssetDatabase/Package Manager 初始化，日志伴随许可证 entitlement 错误，未获得成功编译结果。已核对命令行并停止本轮专用批处理进程；Unity Hub 已打开，账号及许可证由用户本人操作。

## 下一步

1. 用户运行 `D:\Unity\Installers\UnitySetup-Android-Support-for-Editor-6000.0.62f1.exe`；Windows UAC 提示中核对发布者为 Unity Technologies SF，再自行批准。安装目标应对应 `D:\Unity\Hub\Editor\6000.0.62f1`。
2. 恢复可访问 Google 官方 Android 下载站点的网络后补齐 SDK 和 NDK。此版本应使用 NDK r27c、JDK 17、SDK Build Tools 36、Command-line Tools 16、Platform Tools 36；不要套用旧缓存中 r23b 的组件。匹配依据：[Unity 官方依赖版本表](https://docs.unity3d.com/6000.0/Documentation/Manual/android-supported-dependency-versions.html)。
3. 在 Unity Hub 中由用户本人完成账号登录、许可证激活。再导入 `unity/` 工程并检查编译、场景和 APK。

不要直接反复执行旧的全量 Hub 安装命令：本次 `install-modules` 返回 No modules found for this editor，完整 `install` 又返回 Editor already installed in this location。编辑器可启动，但 Hub 的模块管理登记未恢复；暂采用匹配版本的官方单独安装包与工具组件，不伪造模块安装状态。

## 本轮已验证文件

- 编辑器：`D:\Unity\Hub\Editor\6000.0.62f1\Editor\Unity.exe`。
- Java：`D:\Unity\Hub\Editor\6000.0.62f1\Editor\Data\PlaybackEngines\AndroidPlayer\OpenJDK\bin\java.exe`。
- Android 支持安装包：`D:\Unity\Installers\UnitySetup-Android-Support-for-Editor-6000.0.62f1.exe`，500,904,816 字节，Authenticode Valid（Unity Technologies SF），MD5 `94a7348361e23bdb96aef52a62d1f7aa` 与 Hub 官方版本清单一致。
- JDK 完整下载包保存在 `D:\Unity\Installers\Android`。官方组件缓存及本轮日志保存在项目被 Git 忽略的 `.tools/` 中，不作为编译或真机验收证据。

## 上轮安装记录（已结束）

- 编辑器安装由 Unity Hub headless 任务执行，输出在 `.tools/unity-install.stdout.log`，错误输出在 `.tools/unity-install.stderr.log`。
- 后续 Android 子模块任务已执行并失败，输出在 `.tools/unity-modules.stdout.log` 和 `.tools/unity-modules.stderr.log`。错误为 No modules found for this editor，原因是编辑器安装未由 Hub 正常完成登记。
- .NET 3.5 由 Unity 官方安装器调用 `dism.exe /NoRestart /online /enable-feature /featurename:NetFx3 /all` 安装，系统日志位于 `C:\Windows\Logs\DISM\dism.log` 和 `C:\Windows\Logs\CBS\CBS.log`。
- 未强杀 DISM 或 Windows 系统服务。没有自动重启电脑。用户重启后应先核验系统组件及安装器状态，再恢复安装，避免多个安装器同时操作同一目录。
- 需要等待 NetFx3、编辑器及模块安装结束，再核验 Unity.exe、adb、NDK、Java，以及登录 Unity Hub 和许可证激活。当前未验证 C# 编译或 APK 构建。

## 首次安装尝试记录

- 常见安装位置、卸载注册项及进程均未发现 Unity Hub/Editor。
- D 盘有足够空间，计划安装路径为 `D:\Unity\Hub\Editor`。
- 官方新 Hub 下载地址：<https://public-cdn.cloud.unity3d.com/hub/prod/UnityHubSetup-x64.exe>。
- 本次请求此 CDN 连接超时；官方更新清单、编辑器下载连接也发生超时，官方版本 API 连接被关闭；Edge 下载导航超时。
- 没有修改网络、代理、安全设置，没有使用第三方重打包安装器。

## 使用本地安装包继续的记录

- 用户已下载 Hub 官方安装包。验证签名为 Valid，签发主体为 Unity Technologies SF；静默安装退出码 0。
- Hub 安装位置：`C:\Program Files\Unity Hub\Unity Hub.exe`。
- 编辑器安装位置：`D:\Unity\Hub\Editor\6000.0.62f1`。
- 安装中发现旧重复 headless 实例，已核对进程命令后停止旧实例，保留有完整输出日志的当前实例。
- 最初安装停留在 installing 状态。进一步检查进程树发现 DISM 子进程；读取 DISM/CBS 日志确认是 NetFx3 在线下载安装等待，而非继续解压编辑器。
- 编辑器安装包已额外保存至 `D:\Unity\Installers\UnitySetup64-6000.0.62f1.exe`，大小 4,086,441,704 字节，与下载源文件大小一致，供后续恢复时复用。该保存动作不等于编辑器安装完成。

## 网络恢复后继续

以下为首次安装脚本，仅供全新环境参考。当前环境已有编辑器，请按上面的“下一步”恢复，避免再次触发已安装路径错误：

```powershell
& .\scripts\install-unity.ps1
```

脚本下载后先验证 Unity Authenticode 数字签名，再执行安装；安装完成检查 Unity.exe、adb.exe、NDK 和 java.exe。脚本使用与工程基线一致的 Unity 6000.0.62f1，并通过官方 Hub CLI 请求 Android 三项模块。脚本尚未完成真实端到端执行；新 Hub 如不再支持旧 CLI，应根据实际输出改用 Hub 界面或官方 Unity CLI。

如触发 Windows 管理员授权，由用户本人处理。Unity 账号登录和许可证激活也由用户本人完成。环境安装不等于项目编译或 AR 真机验收通过。

也可以通过 Hub 界面安装 Unity 6.0 LTS 同系列适用补丁，勾选 Android Build Support、Android SDK & NDK Tools、OpenJDK。编辑器与模块必须匹配。
