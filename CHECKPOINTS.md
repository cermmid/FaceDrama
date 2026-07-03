# Checkpointy projektu

Punkty w historii, do których łatwo wrócić. Zdalne repo nie przyjmuje tagów,
więc checkpointy zapisujemy tu jako hashe commitów.

| Checkpoint | Commit | Data | Zawartość |
|---|---|---|---|
| **checkpoint-1** | `9014cc0` | 2026-06-13 | Starter kit: README + docs etapów 0–4 + skrypty `UnityStarter/` + kreator konfiguracji (Etap 1 jednym kliknięciem) |
| **checkpoint-2** | `b449d80` | 2026-06-14 | Etap 1 potwierdzony u użytkownika: fix skali wag blend shape'ów (GLB 0..1), avatar mruga poprawnie, sceneria "polana z jeziorem" w kreatorze |

## Jak wrócić do checkpointa

Podejrzeć stan (bez zmiany gałęzi):

```bash
git checkout 9014cc0        # rozejrzyj się; powrót: git checkout claude/vr-therapy-avatar-ljcxne
```

Cofnąć gałąź do checkpointa, ale zachować późniejszą historię (bezpieczne, zalecane):

```bash
git revert --no-commit 9014cc0..HEAD && git commit -m "Powrót do checkpoint-1"
```

Twardy reset (kasuje wszystko po checkpoincie — tylko gdy na pewno):

```bash
git reset --hard 9014cc0
```
