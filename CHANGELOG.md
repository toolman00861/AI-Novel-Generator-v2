# Changelog

## 2025-10-23

### Added
- Local persistence for generated articles using SQLite.
- New service `client/AINovelStudio/Services/NovelStorageService.cs`:
  - Ensures schema for `Novels` and `Chapters`.
  - Provides `EnsureNovel`, `EnsureChapter`, and `SaveGeneratedContent` APIs.
- Integrated persistence into `AIGenerationViewModel.SaveToChapter()` to write chapter content into DB.

### Improved
- User feedback on save success/failure in AI generation page.

### Notes
- Build succeeded with existing nullable warnings (unrelated to this feature).
- File export (`SaveToFile`) remains optional; primary path is local DB persistence.