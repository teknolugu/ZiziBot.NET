using System;
using System.Linq;
using System.Threading.Tasks;
using Humanizer;
using MoreLinq;
using WinTenDev.Zizi.Models.Dto;
using WinTenDev.Zizi.Models.Enums;
using WinTenDev.Zizi.Models.Types;
using WinTenDev.Zizi.Utils;
using WinTenDev.Zizi.Utils.Telegram;

namespace WinTenDev.Zizi.Services.Telegram.Extensions;

public static class TelegramServiceNotesExtension
{
    public static async Task GetNotesAsync(this TelegramService telegramService)
    {
        var chatId = telegramService.ChatId;

        var notesData = await telegramService.NotesService.GetNotesByChatId(chatId);

        var htmlMessage = HtmlMessage.Empty;

        if (notesData.Any())
        {
            htmlMessage.Bold("Catatan di Obrolan ini.").Br().Br();

            notesData.ForEach(
                (
                    tag,
                    index
                ) => {
                    htmlMessage.Code(tag.Id.ToString()).Text(" | ").Text(tag.Tag).Br();
                }
            );
        }
        else
        {
            htmlMessage.Text(
                "Tidak ada Catatan di Obrolan ini." +
                "\nUntuk menambahkannya ketik /add_note"
            );
        }

        await telegramService.SendTextMessageAsync(htmlMessage.ToString(), replyToMsgId: 0);
        await telegramService.NotesService.UpdateCache(chatId);
    }

    public static async Task PrepareSaveNotesAsync(this TelegramService telegramService)
    {
        var chatId = telegramService.ChatId;
        var fromId = telegramService.FromId;
        var message = telegramService.MessageOrEdited;
        var messageTextPart = message.Text.GetTextWithoutCmd().Split("\n\n");

        if (message.ReplyToMessage == null)
        {
            await telegramService.SendTextMessageAsync("Balas salah satu pesan untuk disimpan sebagai Notes");

            return;
        }

        var replyToMessage = message.ReplyToMessage;
        var slugNote = messageTextPart.ElementAtOrDefault(0);
        var buttonData = messageTextPart.ElementAtOrDefault(1);
        var noteContent = replyToMessage.CloneText();
        var fileId = replyToMessage.GetFileId();
        var fileType = replyToMessage.Type.Humanize().Pascalize();

        if (slugNote.IsNullOrEmpty())
        {
            await telegramService.SendTextMessageAsync("Tentukan slug/judul untuk catatan ini");
            return;
        }

        if (await telegramService.NotesService.IsExistAsync(chatId, slugNote))
        {
            await telegramService.SendTextMessageAsync("slug sudah ada, silakan gunakan slug lain untuk Catatan ini.");
            return;
        }

        var saveNote = new NoteSaveDto
        {
            ChatId = chatId,
            FromId = fromId,
            Tag = slugNote,
            Content = noteContent,
            BtnData = buttonData,
            TypeData = fileType,
            FileId = fileId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await telegramService.SendTextMessageAsync("Sedang menyimpan catatan..");
        await telegramService.NotesService.SaveNoteAsync(saveNote);

        await telegramService.EditMessageTextAsync(
            "Catatan berhasil disimpan." +
            "\nKetik <code>/notes</code> untuk melihat catatan."
        );
    }

    public static async Task FindNoteAsync(this TelegramService telegramService)
    {
        var chatId = telegramService.ChatId;
        var messageText = telegramService.MessageOrEditedText;

        var notes = await telegramService.NotesService.GetNotesByChatId(chatId);
        var selected = notes.FirstOrDefault(tag => messageText.Contains(tag.Tag, StringComparison.CurrentCultureIgnoreCase));

        if (selected == null) return;

        var buttonMarkup = selected.BtnData.ToButtonMarkup();

        if (selected.TypeData == MediaType.Text)
        {
            await telegramService.SendTextMessageAsync(
                sendText: selected.Content,
                replyMarkup: buttonMarkup,
                disableWebPreview: true
            );
        }
        else
        {
            await telegramService.SendMediaAsync(
                selected.FileId,
                selected.TypeData,
                caption: selected.Content,
                replyMarkup: buttonMarkup
            );
        }
    }

    public static async Task DeleteNoteAsync(this TelegramService telegramService)
    {
        if (!await telegramService.CheckUserPermission())
        {
            await telegramService.DeleteSenderMessageAsync();
            return;
        }

        var chatId = telegramService.ChatId;
        var slugOrId = telegramService.MessageOrEditedText.GetTextWithoutCmd();

        var delete = await telegramService.NotesService.DeleteNoteAsync(chatId, slugOrId);
        var deleteResult = delete > 0
            ? "Catan berhasil dihapus"
            : "Catan tidak ditemukan";

        await telegramService.SendTextMessageAsync(
            deleteResult,
            scheduleDeleteAt: DateTime.UtcNow.AddMinutes(3),
            includeSenderMessage: true
        );
    }
}