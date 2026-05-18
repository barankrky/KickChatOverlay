# Kick Chatroom ID'sini Manuel Bulma

Eğer uygulama otomatik olarak chatroom ID'sini çözemiyorsa (Cloudflare engeli vb.), aşağıdaki yöntemlerle manuel olarak bulup uygulama ayarlarına girebilirsiniz.

## Yöntem 1: curl ile (Önerilen)

```powershell
curl.exe -s -c cookies.txt "https://kick.com/" -o NUL
curl.exe -s -b cookies.txt "https://kick.com/api/v2/channels/KANAL_ADI" | ConvertFrom-Json | Select-Object -ExpandProperty chatroom
```

`KANAL_ADI` yerine kendi kanal adınızı yazın. Çıktıdaki `id` alanı chatroom ID'sidir.

## Yöntem 2: Tarayıcı Developer Tools ile

1. Tarayıcınızda `https://kick.com/KANAL_ADI` adresine gidin
2. **F12** ile Developer Tools'u açın
3. **Network** sekmesine tıklayın
4. Sayfayı yenileyin (F5)
5. Filtre kutusuna `channels/` yazın
6. `channels/KANAL_ADI` isteğine tıklayın
7. **Response** sekmesinde JSON yanıtını görün
8. `chatroom.id` değerini not alın

Örnek yanıt:
```json
"chatroom": {
    "id": 24783448,
    "chat_mode": "public",
    ...
}
```

Bu ID'yi uygulamanın **Settings > Kick Chatroom ID** alanına girin.
