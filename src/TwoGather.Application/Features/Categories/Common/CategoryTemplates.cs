using TwoGather.Domain.Enums;

namespace TwoGather.Application.Features.Categories.Common;

public static class CategoryTemplates
{
    public sealed record CategoryTemplateItem(string Name, string RoomLabel);

    public static IReadOnlyList<CategoryTemplateItem> GetDefaultsFor(ListTemplate template) => template switch
    {
        ListTemplate.HomeDesign => HomeDesign,
        ListTemplate.Wedding => Wedding,
        _ => Array.Empty<CategoryTemplateItem>()
    };

    private static readonly IReadOnlyList<CategoryTemplateItem> HomeDesign = new[]
    {
        new CategoryTemplateItem("Salon", "Salon"),
        new CategoryTemplateItem("Yatak Odası", "Yatak Odası"),
        new CategoryTemplateItem("Mutfak", "Mutfak"),
        new CategoryTemplateItem("Banyo", "Banyo"),
        new CategoryTemplateItem("Çocuk Odası", "Çocuk Odası"),
        new CategoryTemplateItem("Genel", "Genel"),
    };

    private static readonly IReadOnlyList<CategoryTemplateItem> Wedding = new[]
    {
        new CategoryTemplateItem("Mekan", "Mekan"),
        new CategoryTemplateItem("Kıyafet", "Kıyafet"),
        new CategoryTemplateItem("Catering", "Catering"),
        new CategoryTemplateItem("Davetiye ve Kırtasiye", "Davetiye ve Kırtasiye"),
        new CategoryTemplateItem("Fotoğraf ve Video", "Fotoğraf ve Video"),
        new CategoryTemplateItem("Müzik ve Eğlence", "Müzik ve Eğlence"),
        new CategoryTemplateItem("Genel", "Genel"),
    };
}
