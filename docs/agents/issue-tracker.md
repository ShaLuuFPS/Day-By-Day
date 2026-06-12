# Issue tracker: GitHub (MCP)

Issues 和 PRD 通过 GitHub MCP 直接操作，无需安装 `gh` CLI。

仓库：`ShaLuuFPS/Day-By-Day`

## MCP 工具映射

| 操作 | MCP 工具 | 关键参数 |
|---|---|---|
| 创建 issue | `mcp__github__create_issue` | `owner`, `repo`, `title`, `body`, `labels`, `assignees` |
| 读取 issue | `mcp__github__get_issue` | `owner`, `repo`, `issue_number` |
| 列出 issues | `mcp__github__list_issues` | `owner`, `repo`, `state`, `labels`, `sort` |
| 评论 issue | `mcp__github__add_issue_comment` | `owner`, `repo`, `issue_number`, `body` |
| 更新 issue | `mcp__github__update_issue` | `owner`, `repo`, `issue_number`, `title`, `body`, `state`, `labels` |
| 搜索 issues | `mcp__github__search_issues` | `q`（GitHub 搜索语法） |

其中 `owner` 固定为 `ShaLuuFPS`，`repo` 固定为 `Day-By-Day`。

## 惯例

- **创建 issue**：调用 `mcp__github__create_issue`，必填 `title` 和 `body`。打标签时传入 `labels` 数组。
- **读取 issue**：调用 `mcp__github__get_issue`，用 `issue_number` 定位。
- **列出 issues**：调用 `mcp__github__list_issues`，用 `state` 和 `labels` 过滤。
- **评论**：调用 `mcp__github__add_issue_comment`，`body` 写评论内容。
- **改标签**：调用 `mcp__github__update_issue`，传新的 `labels` 数组。
- **关闭**：调用 `mcp__github__update_issue`，传 `state: "closed"`。

## 当 skill 说 "发布到 issue tracker"

用 `mcp__github__create_issue` 创建 GitHub issue。

## 当 skill 说 "获取相关 ticket"

用 `mcp__github__get_issue` 读取，或 `mcp__github__search_issues` 搜索。
