# 🌊 MoyuBrowser (摸鱼浏览器) 

<p align="center">
  <img src="assets/logo.png" width="128" height="128" />
</p>

<p align="center">
  <img src="https://img.shields.io/badge/.NET-8.0-blue?style=flat-square" />
  <img src="https://img.shields.io/badge/Platform-Windows-lightgrey?style=flat-square" />
  <img src="https://img.shields.io/badge/Engine-WebView2-green?style=flat-square" />
  <img src="https://img.shields.io/badge/License-MIT-orange?style=flat-square" />
</p>

---

## 🏗️ 项目简介 (Description)
**MoyuBrowser** 是一款深度定制的办公隐匿型浏览器。它基于 **Microsoft Edge WebView2 (Chromium)** 内核，融合了多项针对职场环境设计的“隐匿防御”技术，为您在忙碌的商务办公中提供一个私密、高效且极具安全感的个人灵感空间。

> **核心哲学**：Work Hard, Moyu Harder. 让专注回归本质，让放松不留痕迹。

---

## 🛠️ 三段式防御引擎 (The Stealth Engine)

本项目实训的核心是通过深度整合 **Win32 API** 实现的底层隐匿逻辑：

<details>
<summary><b>1. 🚀 全局老板键 (Universal Boss Key)</b></summary>
利用 <code>RegisterHotKey</code> 注册系统级钩子（<code>Alt + B</code> / <code>Alt + Space</code>）。瞬间剥离任务栏图标并移除主窗口视觉句柄，进入进程级“伪休眠”状态，完美应对紧急查岗。
</details>

<details>
<summary><b>2. 🌓 智能焦点淡化 (Focus-Dynamic Stealth)</b></summary>
实时监控窗口焦点状态。当鼠标移出或失去焦点超过 200ms 时，窗体透明度将由 1.0 平滑过渡至极低数值（默认 0.15），与桌面背景完美共生。
</details>

<details>
<summary><b>3. 📜 身份伪装 (Contextual Identity Spoofing)</b></summary>
内置动态标题注入器。允许用户一键将窗口标题注入为 <code>财务审计报告.docx</code>, <code>项目进度计划.xlsx</code> 或 <code>系统架构图.vsdx</code> 等常见办公文件，实现视觉层面的全链路伪装。
</details>

---

## 🎨 动态视觉 Hub & 色彩引擎

我们设计了一套**统一色彩令牌系统**，通过动态属性代理解决了界面各元素间风格撕裂的问题：

- **🌸 樱花粉 (Sakura Pink)**：实训新增的现代极简主题，采用低饱和度莫兰迪粉，赋予技术软件少有的温润感。
- **🌑 专家级暗色模式**：深度适配 VS Code 暗色风格，支持多级悬停反馈与输入区域色彩自适应。
- **🌫️ 极简透明模式**：配合原生毛玻璃效果与动态透明度调节，呈现极具现代感的交互体验。
- **🕶️ 冷静模式 (Global Greyscale)**：基于 Runtime CSS Injection 技术，对网页层及 UI 层进行全局去色，使浏览内容回归纯粹，告别繁杂的色彩干扰。

---

## ⌨️ 专家级快捷键参考

| 动作 | 键位 | 预期效果 |
| :--- | :--- | :--- |
| **瞬间隐身** | `Alt + B` / `Alt + Space` | 瞬间隐藏主窗体及其任务栏标识 |
| **动态亮度** | `Alt + ↑` / `Alt + ↓` | 阶梯式 (10%) 调节主窗体不透明度 |
| **标签管理** | `Alt + T` / `Alt + W` | 快速增减浏览标签页 |
| **极速最小化** | `Alt + Q` | 瞬间收缩至系统托盘，仅保留后台服务 |

---

## 📦 构建与离线部署

项目已实现高度工程化，支持多场景发布交付：

```bash
# 生成单文件绿色便携版 (Single-file Release)
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o ./publish
```

- **[MoyuBrowser.iss](MoyuBrowser.iss)**: 标准 Inno Setup 脚本，用于生成带图标的安装程序。
- **[Install.ps1](Install.ps1)**: PowerShell 一键自动化部署脚本，适用于内网快速分发。

---

> **实训总结 (Reflection)**：
> MoyuBrowser 的开发不仅是对 **C# 12.0** 与 **.NET 8** 特性的实战验证，更是对底层操作系统交互逻辑与现代 Web 容器技术的深度融合尝试。它向我们展示了如何通过优秀的 UI 交互设计与底层逻辑封装，平衡个人隐私保护与职业环境适配。

---

> "Work hard, Moyu harder." 🌊
