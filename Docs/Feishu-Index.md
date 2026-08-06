# 飞书策划案索引

飞书是本项目策划案、需求评审和设计决策的唯一真源。本文件只维护稳定入口与实现状态，不复制完整策划内容。

本地技术流程：[飞书文档读写流程](Feishu-Workflow.md)

## 项目知识库

- 知识空间：[强袭魔女：向天进发（Strike Witches: Road to Skyline）](https://www.feishu.cn/wiki/CkUiwpmU1ihVdvkpeCncxgoQnng)
- Space ID：`7484567522461827073`
- 说明：强袭魔女同人 RTS 游戏
- Bridge profile：`swrts`
- 飞书 Bot：`CDS的智能助手`
- 本地工作区：`D:\UnityProjects\Strike-Witches-Road-to-Skyline`

## 接入状态

- Lark Coding Agent Bridge：`0.6.4`
- Codex CLI：`0.146.0`，已使用 ChatGPT 账号登录
- 飞书 CLI：`1.0.81`，用户 `CDS` 已授权，默认身份为 `user`
- 身份策略：Bridge profile 标记为 `user-default`；CLI 使用更小权限的 `strict-mode=user`
- Windows 后台任务：`LarkChannelBridge.Bot.swrts`，已验证在线
- Codex 文件权限：`workspace-write`
- 群聊策略：默认必须 `@Bot`
- 文档策略：知识库是策划真源；默认可读用户已授权的飞书资源，写入操作仍需用户明确要求
- 写入能力：已授权新建知识库文档节点、创建 Docx 及编辑 Docx 正文；未授权请求不会执行写入
- 安全边界：这是私人 Bot，不开放给无关成员或群聊

## 当前目录

以下目录于 2026-07-31 通过飞书 API 初次读取验证；后续新增条目在链接后标注日期：

- [项目启动](https://www.feishu.cn/wiki/CkUiwpmU1ihVdvkpeCncxgoQnng)
  - [强袭魔女同人RTS游戏企划案](https://www.feishu.cn/wiki/Ai77wIOXPil68wkqj5ycZ5l7nfg)
  - [游戏设计文档](https://www.feishu.cn/wiki/D1SrwGKVYihhGNk7FffcUi0WnKh)
  - [预研阶段（电子表格）](https://www.feishu.cn/wiki/LIjYwGfwyiM5i9kzM4kcPOHrnud)
  - [实现日志](https://www.feishu.cn/wiki/Wa6qwkuc2iK9i7kutCfc4u4JnRD)
  - [《强袭魔女》战役、战场地理与魔女基地资料（关卡制作参考）](https://www.feishu.cn/docx/KEZyd69Opor52kx2uatcM9wQnog)（2026-08-05 新增）
- [项目规划](https://www.feishu.cn/wiki/DRwcwdasui0OWykr8xKcojo5n5b)
- [项目甘特图（电子表格）](https://www.feishu.cn/wiki/Y1IswrLoViV3Idk5EBQc3R6kngb)
- [团队会议](https://www.feishu.cn/wiki/MO5YwKI64iYlu3kbZh4cgAa6nWf)
  - [每日进展同步](https://www.feishu.cn/wiki/JTIJwsEKui0IJgk2mWdcvbcQnrh)
  - [团队周会](https://www.feishu.cn/wiki/NFwTwF7ariZX1tkS4HqcNetTnSe)
- [项目复盘](https://www.feishu.cn/wiki/H52qwW63gi2fMDk5NrOcZCKsn6b)
- [未命名文档](https://www.feishu.cn/wiki/CPxqwNmpDiihC5kn6RBcPGhpnTd)
- [局内系统设计文档](https://www.feishu.cn/wiki/WKmEwEB24iqLQPkFcM5cz3yJntc)
  - [Demo 1.0 设计文档](https://ucn6ha0rjf26.feishu.cn/wiki/VyrVwhI4micluJkXSnBcEVkBnnc)
  - [Demo1 战斗 个体](https://ucn6ha0rjf26.feishu.cn/wiki/X0Ccwvi9ziGsZCkUnUhco85Lndd)（2026-08-07 新增；个体战斗下一版目标规格）

## 实现状态

- Demo 1.0：基于《Demo 1.0 设计文档》revision 6 实现于 `codex/demo1-development`；入口场景为 `Assets/Demo1/Scenes/Demo1.unity`，技术假设与操作说明见 `Assets/Demo1/README.md`。文档中尚未定稿的公式、数值、路径、AI、输入与表现细节均保持为可替换的原型默认值，不作为策划真源。

## 读取验证

- 已列出账号可访问的 3 个知识空间。
- 已遍历本项目知识库根目录及“项目启动”“团队会议”子目录，共 13 个节点。
- 已读取《强袭魔女同人RTS游戏企划案》的目录与“游戏核心”正文，验证文档内容链路可用。
- 默认身份复测无需显式传入 `--as user` 即可读取该知识空间。

## 建议目录

| 编号 | 分类 | 飞书入口 | 说明 |
| --- | --- | --- | --- |
| 00 | 项目总览与文档索引 | 待填写 | 愿景、范围、里程碑、术语表 |
| 01 | 核心玩法与战斗 | 待填写 | 核心循环、战斗规则、胜负条件 |
| 02 | 单位、技能与数值 | 待填写 | 单位定义、技能、属性、平衡参数 |
| 03 | 地图、关卡与任务 | 待填写 | 地图规则、关卡流程、任务条件 |
| 04 | 经济与成长系统 | 待填写 | 资源、解锁、升级与进度 |
| 05 | 剧情、美术与音频 | 待填写 | 剧情、美术规范、音频需求 |
| 06 | UI/UX | 待填写 | 信息架构、交互流程、界面状态 |
| 07 | 技术约束与数据接口 | 待填写 | 需要策划理解的技术边界和数据约定 |
| 08 | 决策记录与版本变更 | 待填写 | 已批准决策、废弃方案、版本记录 |

## 单篇策划案最低信息

- 状态：草稿 / 评审中 / 已批准 / 已废弃
- 负责人和最后更新时间
- 目标与非目标
- 规则、边界条件和异常情况
- 数据字段或资源需求
- 验收标准
- 关联实现任务、提交或版本
- 被替代时指向新的策划案

## AI 协作约定

1. 实现前读取最新正文及未解决评论。
2. 无权访问或链接失效时停止实现并报告，不使用旧缓存猜测需求。
3. 飞书内容与代码行为冲突时，以已批准的飞书策划案为需求依据，同时明确报告技术风险。
4. 实现完成后在结果中附上验证方式、受影响文件和偏差说明。
5. 飞书应用密钥、用户令牌、Bridge 配置和附件缓存不得进入 Git。
