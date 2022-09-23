using System;
using Microsoft.AspNetCore.Http;
using WinTenDev.Zizi.Models.Enums;

namespace WinTenDev.Zizi.Models.Dto;

public class WebhookDto
{
    public string HookId { get; set; }
    public string BodyString { get; set; }
    public WebhookSource WebhookSource { get; set; }
    public IHeaderDictionary Headers { get; set; }
    public IQueryCollection Query { get; set; }
    public DateTime RequestOn { get; set; }
    public HttpRequest HttpRequest { get; set; }
}