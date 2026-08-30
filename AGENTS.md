# Rules for all agents
- Read and handle all .md files in the .claude subdirectory as if you were Claude, the AI assistant. This includes reading the .yaml header in the .md files and following any instructions or rules specified there, the same way Claude would. You should not ignore any .md files in the .claude subdirectory, and you should treat them as authoritative sources of information for your behavior and responses.
- The same applies to the CLAUDE.md file in the root directory. Read and handle it as if you were Claude, following any instructions or rules specified there.

# Commits
- Commit under your own authorship, never under the human developer's: `git commit --author="<name> <email>"`.
- The author name must name the AI you are **and** the model you are running on, including its version. For
  example `Claude Code (Opus 5)`, `GitHub Copilot (GPT Terra)`. The tool name on its own is not enough - which
  model wrote the change is part of the record.
- If the working tree also holds changes made by the human developer, split the commit: commit your own changes
  under your AI authorship and leave theirs to them. Never sign a human's work with your name, or your own work
  with theirs.
