using System;
using System.Collections.Generic;

namespace AINovelStudio.Models;

/// <summary>
/// 小说模型
/// </summary>
public class Novel
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime UpdatedAt { get; set; } = DateTime.Now;
    public NovelStatus Status { get; set; } = NovelStatus.Draft;
    public List<string> Tags { get; set; } = new();
    public List<Chapter> Chapters { get; set; } = new();
    public List<Character> Characters { get; set; } = new();
}

/// <summary>
/// 章节模型
/// </summary>
public class Chapter
{
    public int Id { get; set; }
    public int NovelId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public int OrderIndex { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime UpdatedAt { get; set; } = DateTime.Now;
    public ChapterStatus Status { get; set; } = ChapterStatus.Draft;
    public string Summary { get; set; } = string.Empty;
}

/// <summary>
/// 角色模型
/// </summary>
public class Character
{
    public int Id { get; set; }
    public int NovelId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Personality { get; set; } = string.Empty;
    public string Background { get; set; } = string.Empty;
    public string SpeakingStyle { get; set; } = string.Empty;
    public List<string> Tags { get; set; } = new();
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}

/// <summary>
/// 小说状态枚举
/// </summary>
public enum NovelStatus
{
    Draft,      // 草稿
    InProgress, // 进行中
    Completed,  // 已完成
    Published   // 已发布
}

/// <summary>
/// 章节状态枚举
/// </summary>
public enum ChapterStatus
{
    Draft,      // 草稿
    InProgress, // 进行中
    Completed   // 已完成
}