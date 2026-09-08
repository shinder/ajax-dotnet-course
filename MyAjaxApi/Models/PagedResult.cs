namespace MyAjaxApi.Models;

// 分頁回應的外殼：除了本頁資料，還要告訴前端總筆數與目前頁碼，前端才能畫出分頁按鈕。
// 序列化成 JSON 後屬性會轉成 camelCase：{ "items": [...], "total": 12, "page": 1, "size": 5 }
public class PagedResult<T>
{
    public IReadOnlyList<T> Items { get; init; } = [];
    public int Total { get; init; }
    public int Page { get; init; }
    public int Size { get; init; }
    public int TotalPages => (int)Math.Ceiling(Total / (double)Math.Max(Size, 1));
}
