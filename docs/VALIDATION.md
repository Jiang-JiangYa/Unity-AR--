# 实际验证记录

## 2026-11-02 提交弹窗改动

- 提交中、成功、失败及参数错误统一使用居中弹窗；结果点 OK 关闭。成功显示 ID，失败保留输入和标记。全屏遮罩及控制器状态检查禁止背景点击、AR 放置和重复提交。
- Unity 实际 C# 编译通过。Editor Play 中空标题提交出现 Cannot submit / Title is required；点击遮罩后的背景 Priority 按钮，弹窗不关闭且优先级保持 medium；点击 OK 后恢复表单。
- 新版 Android APK 实际构建成功，产物 `unity/Builds/inspection.apk`，25,277,467 字节。此产物覆盖了此前的 APK，下面历史记录的大小和哈希只对应当时版本。
- 首次覆盖安装返回 `INSTALL_FAILED_ABORTED: User rejected permissions`；用户要求继续后，实际安装返回 Success，USB 8080 转发恢复，UnityPlayerGameActivity 已启动，后端 /health 返回 ok。新版已安装；真机成功/失败弹窗及真实 AR 上报仍待现场复测，不得将编辑器的空标题检查写成真实 AR 上报闭环通过。

日期：2026-10-28（Asia/Shanghai）。所有测试数据使用题目示例，不含真实园区或账号信息。

## 已完成

1. Go 1.27.3 / Windows amd64：`go test -v ./...`，6 组通过：
   - TestLifecycleAndRestart：创建、默认状态、坐标、三种状态更新、重新打开文件读取。
   - TestValidation：空标题、错误枚举、缺失或 null 坐标、坐标类型错误、非法 JSON、多个对象及不存在的 ID。
   - TestCORS：允许来源的预检响应，其他来源不返回授权头。
   - TestConcurrentWrites：24 个并发创建，写入无丢失，ID 不重复，重新读盘正常。
   - TestPersistenceFailureDoesNotPublish：创建和更新落盘失败时内存不变，返回 500。
   - TestCorruptFileIsPreserved：损坏 JSON 不被覆盖。
2. `go vet ./...` 通过；后端可执行文件构建成功。
3. TypeScript 5.9.3 `tsc -b` 通过，Vite 7.3.7 生产构建通过。
4. pnpm 11.25.0 `install --frozen-lockfile --offline` 成功，esbuild 初始化成功。实际 React 版本由 `pnpm-lock.yaml` 锁定。
5. 使用 Playwright + 本机 Microsoft Edge 无头浏览器，连接实际 Go 进程，完成：
   - 空列表展示。
   - 通过真实 POST API 创建示例问题（此操作模拟 Unity 请求，不能算真机上报）。
   - 页面刷新后展示中文标题、优先级、坐标；实际跨域请求成功。
   - 通过页面选择框修改为 in_progress、resolved，向 API 读取核对。
   - 杀掉并重新启动 Go 进程，ID 与 resolved 状态保留。
   - 停止 Go 后刷新，出现“无法连接后端”提示；重启后点击“重试”恢复。
   - 停止 Go 后修改状态，出现错误且界面保留已确认状态。
   - 全程没有捕获到浏览器 pageerror。
6. 已人工查看管理端正常页面截图，布局及中文显示正常。
7. 2026-10-31：Unity 6000.0.62f1 导入真实工程，Input System 1.11.2、ARCore 6.0.8 下载进入 PackageCache；Editor.log 显示 `Tundra build success`，生成 Assembly-CSharp.dll 与 Assembly-CSharp-Editor.dll，C# 编译通过。项目 manifest 的 registry 已设置为 Unity 官方中国区 https://packages.unity.cn，应对本机曾出现的 ENOTFOUND；未修改依赖版本及系统网络配置。
8. 2026-10-31：真实 Android 构建发现 Both 输入模式不受支持，取消首轮构建并修正为统一新版 Input System（Touchscreen + InputSystemUIInputModule + TrackedPoseDriver），再次构建成功。APK 为 `unity/Builds/inspection.apk`，25,275,219 字节；Editor.log 明确记录 `APK_BUILD_SUCCEEDED`。SHA256：`18453E02A811F22DC8370149BD4321579F94D6457CE2C0B1B5492E2984302620`。
9. 手机 ADB 状态 device，Android 13、Google AR 服务存在；场景与 ARCore loader 配置已核验。后端从 backend 工作目录启动，/health 返回 ok，USB reverse 8080 已建立。首次安装返回 `INSTALL_FAILED_ABORTED: User rejected permissions`；用户随后确认安装完成，并反馈已看到绿色平面和紫色球形标记。这是用户现场反馈，未记录完整手机上报和网页状态闭环证据；紫色显示仍待排查。

截图：[正常状态](admin-preview.png)、[后端断开](admin-error-preview.png)。截图使用独立测试数据文件，不预填到正式 `backend/data/issues.json`。

## 环境问题与处理

- 系统 npm 内部 minipass 依赖异常；改用应用自带 pnpm，未修改系统 npm。
- 沙箱默认临时目录禁止 Go 重命名；使用项目 `.tools/test-temp` 作为 TEMP/TMP 后测试通过。
- 本机网络监听与浏览器验证在获准的非沙箱执行环境完成，测试服务只绑定 loopback，测试完成后停止。
- 第一次 pnpm 安装要求显式允许 esbuild 构建脚本；已用项目级 `pnpm-workspace.yaml` 仅允许 esbuild，再验证冻结锁文件安装。

## 尚未完成（提交前必须补验）

- 2026-10-31 实际运行的编辑器位于 C:\Program Files\Unity\Hub\Editor\6000.0.62f1；SDK adb、NDK（27.2.12479018）、JDK 与真实 APK 构建已验证，Inspection 场景已生成。
- 用户已确认手机安装及绿色平面、球形标记显示；逐项真机验收尚未记录。紫色球显示问题、UI 触摸防误放、失败提示和手机现场上报至网页修改状态的闭环仍需补验。
- 当前证明 Go / React 链路、Unity 编译和 APK 构建，**不证明整题已经通过真机验收**。
- 按 `ACCEPTANCE.md` 完成真机与三端联调，再补充设备、Unity 版本、测试时间和结果。
