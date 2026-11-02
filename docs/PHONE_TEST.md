# 手机现场测试

本页是操作步骤，实际通过情况填写到 ACCEPTANCE.md；不要把步骤当作通过结果。

## 连接

1. 手机通过 USB 连接电脑并允许 USB 调试，保持连接。
2. 启动同一 Go 后端（工作目录 backend），浏览器访问 http://localhost:8080/health 应返回 ok。
3. 执行 `adb reverse tcp:8080 tcp:8080`。手机断开重连或 ADB 重启后需要再次执行。
4. 安装 APK 后打开手机上的 **Park Inspection**，由本人允许相机权限。Backend URL 保持 `http://127.0.0.1:8080`。

## 放置与上报

1. 在光照充足、有纹理的地面或桌面前缓慢移动手机；上方应显示 `AR: SessionTracking`，Planes 大于 0，画面出现绿色检测平面。
2. 点击表单上方的绿色平面，出现橙色球和 Marker placed 坐标提示。
3. 点击 Issue title 输入“入口墙面破损”；Description 可填“左侧墙体存在裂缝”。点击输入框、Priority 和表单空白区域时观察球不移动。
4. Priority 按钮循环 medium → high → low → medium。选 high 后点 Submit issue。
5. 提交中出现等待弹窗；成功后弹窗标题为 Submitted successfully，显示问题 ID，当前球及表单标题清除。记下 ID，点 OK 关闭。失败或参数不完整也使用弹窗，确认后可继续修改。

## 网页闭环

1. 电脑打开 http://localhost:5173/；若之前停过后端，点“重试”或刷新。
2. 查找同一 ID 的问题，核对标题、优先级、状态 open 与坐标。
3. 将状态改为 in_progress，再改 resolved，刷新确认保留。
4. 在同一 backend 工作目录重启 Go，再核对该 ID 和状态；本机数据保存到 backend/data/issues.json。

## 失败路径

- 未放标记、空标题和纯空格标题应明确阻止上报。
- 弹窗打开时，点击遮罩和按钮不能在 AR 中放置或移动标记；提交中不能重复请求，结果弹窗点 OK 关闭后才能继续操作。较长错误信息可在弹窗内容区域上下滚动。
- 后端地址临时填 `http://127.0.0.1:8081`，提交应提示失败，保留已填内容；改回 8080 后重试。不要使用断开 USB 代替此测试，因为 USB 同时承载网络。
- 超时有可能后端已保存，先查网页再决定是否重试，避免重复问题。
- 相机无实时画面、状态不是 SessionTracking、Planes 一直为 0 时，记录界面状态和 Unity 应用日志，不声称 AR 验收通过。
