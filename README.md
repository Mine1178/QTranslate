<div align="center">

<h1 align="center">QTranslate</h1>

<p>一款轻量级 Windows 桌面翻译工具，基于 <a href="https://github.com/STranslate/STranslate">STranslate</a> 二次开发。</p>

<p>
<img alt="Platform" src="https://img.shields.io/badge/platform-Windows%20x64-blue" />
<img alt=".NET" src="https://img.shields.io/badge/.NET-10.0-purple" />
<img alt="License" src="https://img.shields.io/badge/license-MIT-green" />
</p>

</div>

## ✨ 特性

- **划词翻译**：选中文字自动弹出翻译结果
- **输入翻译**：手动输入文本翻译
- **多翻译引擎**：百度、DeepL、Google、OpenAI、微软、有道、Yandex 等
- **TTS 朗读**：集成 Microsoft Edge TTS
- **生词本 / 历史记录**：完整的记录与收藏功能
- **多主题换肤**：支持多种 UI 主题
- **全局快捷键**：自定义触发快捷键
- **插件系统**：支持拖拽 `.spkg` 文件安装扩展
- **备份 / 恢复**：配置一键迁移

## 📦 下载

前往 [Releases](https://github.com/your-username/QTranslate/releases) 下载最新版本：

| 版本 | 说明 |
|---|---|
| **QTranslate-vX.X.X-setup.exe** | 安装包（推荐） |
| QTranslate-便携版 | 绿色免安装，需 .NET 10 桌面运行时 |
| QTranslate-单文件版 | 自带运行时，双击即用 |

首次运行若缺少运行时，程序会自动弹窗引导通过 winget 安装。

## 📖 使用手册

- [在线阅读（Markdown）](./docs/QTranslate-使用手册.md)
- [PDF 下载](./docs/QTranslate-使用手册.pdf)

## 🛠 从源码构建

前置要求：.NET 10 SDK

```bash
git clone https://github.com/your-username/QTranslate.git
cd QTranslate/src
dotnet build STranslate/STranslate.csproj -c Release
```

发布单文件版：

```bash
dotnet publish STranslate/STranslate.csproj -c Release -r win-x64 --self-contained \
  -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true \
  -p:EnableCompressionInSingleFile=true
```

## 🙏 致谢

本项目基于 [zggsong/STranslate](https://github.com/STranslate/STranslate) 二次开发，感谢原作者的开源贡献。

## 📄 License

[MIT](./LICENSE)
