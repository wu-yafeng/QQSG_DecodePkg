# QQSG_DecodePkg

QQ三国拆包工具源码

用于拆QQ三国的pkg格式的文件

# 使用方法

运行 `DecodePkg.exe` 文件，拖动QQ三国目录下的 `data\\objects.pkg` 文件到控制台，文件将解压到 `%TMEP%/qqsg_objects` 目录下.

# 如果无法运行

请下载 [.NET 6运行时](https://dotnet.microsoft.com/zh-cn/download/dotnet/thank-you/runtime-6.0.16-windows-x64-installer)

# 更新日志

2023/04/20 增加了解密，现在回馈的积分奖励和一些加密的内容也能被拆出来了。优化了console的交互方式

2023/05/23体验服更新中对lua脚本进行了加密,所以在2023/05/24日增加了对lua包的解密逻辑

# Build

运行 `./dotnet-install.ps1` 安装donet sdk

运行 `./build.ps1` 编译代码，输出目录为 ./bin