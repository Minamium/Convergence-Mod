# Source Evidence Template

Use this full template for design-changing findings, cross-Mod comparisons, or copying/provenance questions. A narrow official-API lookup uses the short record described in SKILL.md. Shared repository/version/license details may be recorded once per survey and linked from each independently verifiable finding.

```markdown
## Finding title

- Question:
- Search scope and terms:
- Project and official repository:
- Maintainer/authority evidence:
- License and license URL:
- Source access status: verified / unavailable / blocked / rate-limited
- Target tag/commit/branch:
- Declared Terraria/tModLoader/Calamity target and evidence:
- Exact file or documentation URL:
- Type/member/line anchor:
- Accessed: YYYY-MM-DD
- Confirmed observation:
- Inference:
- Compatibility with pinned tModLoader/Calamity:
- Convergence decision: adopt / adapt / reject / prototype
- Copying boundary: API name only / behavior only / independently reimplemented
- Required tests:
```

## Evidence quality

Prefer, in order:

1. Pinned release source and official API documentation.
2. Official repository source at a named commit.
3. Official issue or pull request with maintainer confirmation.
4. Reproducible local experiment with versioned logs.
5. Community discussion, clearly labeled as secondary.

If a claim depends on more than one file, link each file and explain the call path. If the repository has no explicit license, record that fact and use behavior only as a research lead.

For mutable documentation, include both the current documentation URL and a pinned source/tag URL when the claim affects compatibility or authority. For an unavailable URL, preserve the attempted URL and date, record the failure mode, and do not promote the finding above the strongest source actually inspected.
