using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Humanizer;
using MoreLinq;
using WinTenDev.Zizi.Models.Dto;
using WinTenDev.Zizi.Models.Enums;
using WinTenDev.Zizi.Models.Enums.Languages;
using WinTenDev.Zizi.Models.Types;
using WinTenDev.Zizi.Services.Telegram;
using WinTenDev.Zizi.Utils;
using WinTenDev.Zizi.Utils.Telegram;

namespace WinTenDev.Zizi.Services.Extensions;

public static class TelegramServiceNotesExtension
{
    public static async Task GetNotesAsync(
        this TelegramService telegramService,
        bool useTags = false
    )
    {
        var htmlMessage = HtmlMessage.Empty;
        var chatId = telegramService.ChatId;

        var notesData = await telegramService.NotesService.GetNotesByChatId(chatId);
        var filteredNotes = notesData.Where(
            tag => {
                if (!useTags) return true;

                return !tag.Tag.Contains(' ');
            }
        ).ToList();

        var placeHolders = new List<(string placeholder, string value)>()
        {
            ("Name", useTags ? "Tags" : "Notes"),
            ("ChatTitle", telegramService.ChatTitle)
        };

        if (filteredNotes.Count > 0)
        {
            var notesTitleTr = await telegramService.GetLocalization(Notes.Title, placeHolders);
            htmlMessage.Bold(notesTitleTr).Br().Br();

            filteredNotes.ForEach(
                (
                    tag,
                    _
                ) => {
                    if (useTags)
                        htmlMessage.Text("#").Text(tag.Tag).Text(" ");
                    else
                        htmlMessage.Code(tag.Id.ToString()).Text(" | ").Text(tag.Tag).Br();
                }
            );
        }
        else
        {
            var noNotesTr = await telegramService.GetLocalization(Notes.NoNotes, placeHolders);
            htmlMessage.Text(noNotesTr);
        }

        await telegramService.SendTextMessageAsync(
            sendText: htmlMessage.ToString(),
            replyToMsgId: 0,
            scheduleDeleteAt: DateTime.UtcNow.AddDays(1),
            includeSenderMessage: true,
            preventDuplicateSend: true
        );

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

        if (messageText.IsNullOrEmpty()) return;

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
