# 第三方声明

ZoneTray 由两部分组成，许可不同。

## 1. 托盘宿主与本项目源码

MIT License，见 [LICENSE](LICENSE)。版权归 © 2026 kelisidina725。

托盘宿主、设置面板、模板切换逻辑均为独立实现，没有复制任何第三方托盘项目的源码。

## 2. Microsoft PowerToys FancyZones 官方二进制

发布包 `deployment/src/` 下有三个从 PowerToys 0.99.1 提取的二进制：

- `PowerToys.FancyZones.exe`
- `PowerToys.Interop.dll`
- `PowerToys.GPOWrapper.dll`

这些文件由 Microsoft Corporation 开发并签名，采用 MIT License：

> Copyright (c) Microsoft Corporation. All rights reserved.
>
> Permission is hereby granted, free of charge, to any person obtaining a copy
> of this software and associated documentation files (the "Software"), to deal
> in the Software without restriction, including without limitation the rights
> to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
> copies of the Software, and to permit persons to whom the Software is
> furnished to do so, subject to the following conditions:
>
> The above copyright notice and this permission notice shall be included in
> all copies or substantial portions of the Software.
>
> THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
> IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
> FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
> AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
> LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
> OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
> SOFTWARE.

许可证全文：https://github.com/microsoft/PowerToys/blob/main/LICENSE

本仓库**不存放**这些二进制文件。它们只出现在构建脚本生成的发布包中，用于让 FancyZones 引擎脱离 PowerToys 主程序运行。构建脚本会从你本机安装的 PowerToys 提取，或从已有发布包复用。

## 3. 未包含的内容

FancyZones 布局编辑器及其 .NET/WPF 运行时组件未随发布包分发。分区布局改用内置模板实现。
