# 云统计接入

本模组已内置 `PostHog` 云统计上报。

首次进入主菜单时，模组会弹出匿名统计说明，只有玩家明确选择“启用收集”后才会开始上报。

## 推荐原因

- `PostHog Cloud` 支持事件统计和唯一用户统计
- 适合做 `启动人数 / 次数`、`猪角色使用人数 / 次数`、`胜负次数`
- 代码里直接走 HTTP 上报，不依赖额外 SDK

## 配置文件

首次启动后，模组会自动在游戏用户目录生成配置模板：

`<Slay the Spire 2 user data>/mod_configs/YuWanCard/posthog.analytics.yaml`

如果模组目录下存在 `posthog.analytics.local.yaml`，会优先读取它。
这个文件适合放私有 `projectApiKey`，并且已经被 `.gitignore` 忽略。

如果之前已经使用过旧版 `posthog.analytics.json` / `posthog.analytics.local.json`，模组会在下次启动时自动迁移到 YAML。

示例：

```yaml
enabled: true
host: "https://us.i.posthog.com"
projectApiKey: "phc_xxxxxxxxxxxxxxxxx"
captureLaunchEvents: true
captureRunEvents: true
sendPersonProfiles: true
```

## 当前上报事件

- `mod_session_started`
- `run_started`
- `run_ended`

## 关键字段

- `distinct_id`: 本地自动生成的匿名唯一 ID，用来统计人数
- `character_id`
- `character_type`
- `is_pig`
- `result`
- `player_count`
- `ascension_level`
- `mod_version`

## 可直接做的统计

- `mod_session_started` 的唯一 `distinct_id` 数：安装 mod 后启动游戏的人数
- `mod_session_started` 的事件数：启动次数
- 过滤 `is_pig = true` 的 `run_started` 唯一 `distinct_id` 数：使用猪角色的人数
- 过滤 `is_pig = true` 的 `run_started` 事件数：使用猪角色的次数
- `run_ended` 中 `result = victory / defeat` 的事件数：胜利 / 失败次数
