<div align="center">

<img src="https://raw.githubusercontent.com/your-username/QTranslate/main/src/STranslate/Resources/app.ico" width="100" height="100" alt="QTranslate"/>

# QTranslate

**⚡ 极致轻量的桌面翻译工具**

[![Platform](https://img.shields.io/badge/Windows-x64-0078D4?logo=windows&logoColor=white)]()
[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet&logoColor=white)]()
[![License](https://img.shields.io/badge/License-MIT-green.svg)]()
[![Release](https://img.shields.io/badge/release-v1.0.1-orange)]()

<p align="center">
  划词即译 · 秒速启动 · 内存仅 50MB
</p>

</div>

---

## ✨ 为什么选择 QTranslate？

| | **QTranslate** | 传统翻译工具 |
|---|---|---|
| 🚀 **启动速度** | 秒开，无等待 | 3-5 秒启动 |
| 💾 **内存占用** | ~50 MB | 200-500 MB |
| 📦 **安装包** | ~18 MB | 100+ MB |
| 🎯 **专注度** | 只做翻译，不臃肿 | 塞满用不上的功能 |
| 🎨 **界面** | 现代 Fluent 设计 | 老旧 Windows 风格 |

> **砍掉 OCR、砍掉向导、砍掉自动更新、砍掉冗余功能**——只留你真正每天都在用的翻译能力。

---

## 🎯 核心功能

<div align="center">

| | | |
|:---:|:---:|:---:|
| 🔤 **划词翻译**<br/>选中即译 | ⌨️ **输入翻译**<br/>手动输入 | 🌐 **多引擎**<br/>百度/DeepL/Google |
| 🔊 **TTS 朗读**<br/>Edge 语音 | 📖 **生词本**<br/>收藏难词 | 🕐 **历史记录**<br/>自动留存 |
| 🎨 **多主题**<br/>浅色/深色 | ⌨️ **快捷键**<br/>全局自定义 | 🧩 **插件系统**<br/>.spkg 扩展 |

</div>

---

## 🚀 快速开始

### 下载

前往 [Releases](https://github.com/your-username/QTranslate/releases) 下载：

| 版本 | 适用场景 |
|---|---|
| **Setup.exe 安装包** ⭐ | 推荐，双击安装 |
| 便携版 | 绿色免安装（需 .NET 10 运行时） |
| 单文件版 | 自带运行时，即下即用 |

### 三步上手

1. **安装 / 解压**
2. 启动后软件安静躺在系统托盘 🔔
3. 选中任意外文 → 自动弹出翻译结果

> 首次运行若缺少 .NET 运行时，会自动弹窗引导一键安装。

---

## ⌨️ 默认快捷键

| 快捷键 | 功能 |
|---|---|
| `Ctrl + Enter` | 呼出输入翻译 |
| `Ctrl + Shift + Q` | 显示/隐藏主窗口 |

> 全部可在「设置 → 快捷键」中自定义。

---

## 📖 使用手册

- [📖 在线阅读（Markdown）](./docs/QTranslate-使用手册.md)
- [📄 PDF 下载](./docs/QTranslate-使用手册.pdf)

---

## 🛠 从源码构建

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

---

## 💝 支持本项目

如果 QTranslate 帮到了你，可以请作者喝杯咖啡 ☕

在软件「关于」页面点击「☕ 赞助作者」查看二维码。

---

## 📄 License

[MIT](./LICENSE)
