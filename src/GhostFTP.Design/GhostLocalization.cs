using System.Globalization;

namespace GhostFTP.Design;

public sealed record GhostLanguage(string Code, string NativeName, string EnglishName)
{
    public override string ToString() => $"{NativeName} — {EnglishName}";
}

public static class GhostLocalization
{
    public const string DefaultLanguageCode = "en";

    // This schema is intentionally initialized before locale dictionaries.
    // A malformed locale must never make the application fail during type initialization.
    private static readonly string[] CoreTranslationKeys =
    [
        "Settings", "About", "Add", "Edit", "Remove", "Connect", "Disconnect", "Upload", "Download", "Refresh",
        "NewFolder", "Rename", "Delete", "Cancel", "Close", "Continue", "Save", "Install", "Update", "Uninstall",
        "Launch", "Host", "Port", "Security", "Username", "Password", "Language", "Appearance", "Dark", "Light",
        "Files", "Local", "Remote", "Transfers", "SavedServers", "QuickConnect", "Setup", "Status",
        "CreateDesktopShortcut", "InstallLocation"
    ];

    private static readonly GhostLanguage[] LanguageList =
    [
        new("en", "English", "English"),
        new("hr", "Hrvatski", "Croatian"),
        new("de", "Deutsch", "German"),
        new("fr", "Français", "French"),
        new("es", "Español", "Spanish"),
        new("it", "Italiano", "Italian"),
        new("pt", "Português", "Portuguese"),
        new("nl", "Nederlands", "Dutch"),
        new("pl", "Polski", "Polish"),
        new("cs", "Čeština", "Czech"),
        new("sk", "Slovenčina", "Slovak"),
        new("sl", "Slovenščina", "Slovenian"),
        new("hu", "Magyar", "Hungarian"),
        new("ro", "Română", "Romanian"),
        new("bg", "Български", "Bulgarian"),
        new("el", "Ελληνικά", "Greek"),
        new("tr", "Türkçe", "Turkish"),
        new("uk", "Українська", "Ukrainian"),
        new("ru", "Русский", "Russian"),
        new("sr", "Srpski", "Serbian"),
        new("bs", "Bosanski", "Bosnian"),
        new("sv", "Svenska", "Swedish"),
        new("da", "Dansk", "Danish"),
        new("no", "Norsk", "Norwegian"),
        new("fi", "Suomi", "Finnish"),
        new("ja", "日本語", "Japanese"),
        new("ko", "한국어", "Korean"),
        new("zh-CN", "简体中文", "Chinese (Simplified)"),
        new("zh-TW", "繁體中文", "Chinese (Traditional)")
    ];

    private static readonly Dictionary<string, string> English = new(StringComparer.Ordinal)
    {
        ["Settings"] = "Settings",
        ["About"] = "About",
        ["Add"] = "Add",
        ["Edit"] = "Edit",
        ["Remove"] = "Remove",
        ["Connect"] = "Connect",
        ["Disconnect"] = "Disconnect",
        ["ConnectSelected"] = "Connect selected",
        ["Upload"] = "Upload",
        ["Download"] = "Download",
        ["Refresh"] = "Refresh",
        ["NewFolder"] = "New folder",
        ["Rename"] = "Rename",
        ["Delete"] = "Delete",
        ["Cancel"] = "Cancel",
        ["Close"] = "Close",
        ["Continue"] = "Continue",
        ["Save"] = "Save",
        ["SaveServer"] = "Save server",
        ["SaveSettings"] = "Save settings",
        ["Install"] = "Install",
        ["Update"] = "Update",
        ["Uninstall"] = "Uninstall",
        ["Launch"] = "Launch",
        ["Host"] = "Host",
        ["Port"] = "Port",
        ["Security"] = "Security",
        ["Username"] = "Username",
        ["Password"] = "Password",
        ["Language"] = "Language",
        ["Appearance"] = "Appearance",
        ["Dark"] = "Dark",
        ["Light"] = "Light",
        ["UseWindowsSetting"] = "Use Windows setting",
        ["Files"] = "Files",
        ["Local"] = "Local",
        ["Remote"] = "Remote",
        ["Transfers"] = "Transfers",
        ["SavedServers"] = "Saved servers",
        ["QuickConnect"] = "Quick connect",
        ["Setup"] = "Setup",
        ["Status"] = "Status",
        ["PrivacyByDesign"] = "Privacy by design",
        ["NoTelemetryTracking"] = "No telemetry · No tracking",
        ["CreateDesktopShortcut"] = "Create a desktop shortcut",
        ["RemoveLocalData"] = "Also remove local settings and saved server profiles",
        ["InstallLocation"] = "Install location",
        ["ConnectedServer"] = "Connected server",
        ["ThisPc"] = "This PC",
        ["PrivateFileTransfer"] = "Private file transfer",
        ["ProfileName"] = "Profile name",
        ["InitialRemotePath"] = "Initial remote path",
        ["RememberPassword"] = "Remember password for this Windows user (DPAPI protected)",
        ["AddServer"] = "Add server",
        ["EditServer"] = "Edit server",
        ["ServerProfile"] = "Server profile",
        ["FileWorkspace"] = "File workspace",
        ["ConfirmDeletes"] = "Ask before deleting local or remote files and folders",
        ["ShowHidden"] = "Show hidden and system items in the local file pane",
        ["KeyboardShortcuts"] = "Keyboard shortcuts",
        ["Details"] = "Details",
        ["OperationFailed"] = "Operation failed",
        ["Offline"] = "Offline",
        ["NotConnected"] = "Not connected",
        ["NoTransfers"] = "No transfers",
        ["ClearFinished"] = "Clear finished",
        ["CancelSelected"] = "Cancel selected",
        ["RetrySelected"] = "Retry selected",
        ["CancelAll"] = "Cancel all",
        ["ClearFilter"] = "Clear filter",
        ["Up"] = "Up",
        ["Home"] = "Home",
        ["Desktop"] = "Desktop",
        ["Documents"] = "Documents",
        ["Downloads"] = "Downloads",
        ["Name"] = "Name",
        ["Type"] = "Type",
        ["Size"] = "Size",
        ["Modified"] = "Modified",
        ["Item"] = "Item",
        ["Direction"] = "Direction",
        ["State"] = "State",
        ["Progress"] = "Progress",
        ["Speed"] = "Speed",
        ["Source"] = "Source",
        ["Destination"] = "Destination",
        ["Open"] = "Open",
        ["OpenExplorer"] = "Open in File Explorer",
        ["CopyFullPath"] = "Copy full path",
        ["CopyRemotePath"] = "Copy remote path",
        ["FilterTooltip"] = "Filter items in the current folder",
        ["LanguageRestart"] = "Language changes are applied after Ghost FTP is restarted.",
        ["EnglishFallback"] = "English is the primary language and fallback for untranslated technical text.",
        ["ReadyInstall"] = "Ready to install.",
        ["ReadyUninstall"] = "Ready to uninstall.",
        ["ExistingInstallUpdate"] = "An existing installation will be updated safely.",
        ["Installing"] = "Installing Ghost FTP…",
        ["Updating"] = "Updating Ghost FTP…",
        ["Removing"] = "Removing Ghost FTP…",
        ["InstalledReady"] = "Ghost FTP is installed and ready to use.",
        ["RemovedSuccessfully"] = "Ghost FTP has been removed successfully.",
        ["OperationCouldNotComplete"] = "The operation could not be completed: {0}",
        ["TlsValidation"] = "TLS certificate validation",
        ["NoTelemetryOrTracking"] = "No telemetry or tracking",
        ["PerUserInstallation"] = "Per-user installation",
        ["SelfContainedRuntime"] = "Self-contained runtime"
    };

    private static readonly Dictionary<string, Dictionary<string, string>> Overrides = new(StringComparer.OrdinalIgnoreCase)
    {
        ["hr"] = D("Postavke","O aplikaciji","Dodaj","Uredi","Ukloni","Poveži se","Prekini vezu","Prenesi na server","Preuzmi","Osvježi","Nova mapa","Preimenuj","Izbriši","Odustani","Zatvori","Nastavi","Spremi","Instaliraj","Ažuriraj","Deinstaliraj","Pokreni","Poslužitelj","Port","Sigurnost","Korisničko ime","Lozinka","Jezik","Izgled","Tamno","Svijetlo","Datoteke","Lokalno","Udaljeno","Prijenosi","Spremljeni serveri","Brzo povezivanje","Instalacija","Status","Stvori prečac na radnoj površini","Mjesto instalacije"),
        ["de"] = D("Einstellungen","Info","Hinzufügen","Bearbeiten","Entfernen","Verbinden","Trennen","Hochladen","Herunterladen","Aktualisieren","Neuer Ordner","Umbenennen","Löschen","Abbrechen","Schließen","Weiter","Speichern","Installieren","Aktualisieren","Deinstallieren","Starten","Host","Port","Sicherheit","Benutzername","Passwort","Sprache","Darstellung","Dunkel","Hell","Dateien","Lokal","Remote","Übertragungen","Gespeicherte Server","Schnell verbinden","Setup","Status","Desktop-Verknüpfung erstellen","Installationsort"),
        ["fr"] = D("Paramètres","À propos","Ajouter","Modifier","Supprimer","Se connecter","Déconnecter","Envoyer","Télécharger","Actualiser","Nouveau dossier","Renommer","Supprimer","Annuler","Fermer","Continuer","Enregistrer","Installer","Mettre à jour","Désinstaller","Lancer","Hôte","Port","Sécurité","Nom d’utilisateur","Mot de passe","Langue","Apparence","Sombre","Clair","Fichiers","Local","Distant","Transferts","Serveurs enregistrés","Connexion rapide","Installation","État","Créer un raccourci sur le bureau","Emplacement d’installation"),
        ["es"] = D("Configuración","Acerca de","Añadir","Editar","Quitar","Conectar","Desconectar","Subir","Descargar","Actualizar","Nueva carpeta","Renombrar","Eliminar","Cancelar","Cerrar","Continuar","Guardar","Instalar","Actualizar","Desinstalar","Iniciar","Host","Puerto","Seguridad","Usuario","Contraseña","Idioma","Apariencia","Oscuro","Claro","Archivos","Local","Remoto","Transferencias","Servidores guardados","Conexión rápida","Instalación","Estado","Crear acceso directo en el escritorio","Ubicación de instalación"),
        ["it"] = D("Impostazioni","Informazioni","Aggiungi","Modifica","Rimuovi","Connetti","Disconnetti","Carica","Scarica","Aggiorna","Nuova cartella","Rinomina","Elimina","Annulla","Chiudi","Continua","Salva","Installa","Aggiorna","Disinstalla","Avvia","Host","Porta","Sicurezza","Nome utente","Password","Lingua","Aspetto","Scuro","Chiaro","File","Locale","Remoto","Trasferimenti","Server salvati","Connessione rapida","Installazione","Stato","Crea collegamento sul desktop","Percorso di installazione"),
        ["pt"] = D("Definições","Sobre","Adicionar","Editar","Remover","Ligar","Desligar","Enviar","Transferir","Atualizar","Nova pasta","Renomear","Eliminar","Cancelar","Fechar","Continuar","Guardar","Instalar","Atualizar","Desinstalar","Iniciar","Host","Porta","Segurança","Utilizador","Palavra-passe","Idioma","Aspeto","Escuro","Claro","Ficheiros","Local","Remoto","Transferências","Servidores guardados","Ligação rápida","Instalação","Estado","Criar atalho no ambiente de trabalho","Local de instalação"),
        ["nl"] = D("Instellingen","Over","Toevoegen","Bewerken","Verwijderen","Verbinden","Verbinding verbreken","Uploaden","Downloaden","Vernieuwen","Nieuwe map","Hernoemen","Verwijderen","Annuleren","Sluiten","Doorgaan","Opslaan","Installeren","Bijwerken","Verwijderen","Starten","Host","Poort","Beveiliging","Gebruikersnaam","Wachtwoord","Taal","Weergave","Donker","Licht","Bestanden","Lokaal","Extern","Overdrachten","Opgeslagen servers","Snel verbinden","Installatie","Status","Bureaubladsnelkoppeling maken","Installatielocatie"),
        ["pl"] = D("Ustawienia","O programie","Dodaj","Edytuj","Usuń","Połącz","Rozłącz","Wyślij","Pobierz","Odśwież","Nowy folder","Zmień nazwę","Usuń","Anuluj","Zamknij","Kontynuuj","Zapisz","Zainstaluj","Aktualizuj","Odinstaluj","Uruchom","Host","Port","Bezpieczeństwo","Nazwa użytkownika","Hasło","Język","Wygląd","Ciemny","Jasny","Pliki","Lokalne","Zdalne","Transfery","Zapisane serwery","Szybkie połączenie","Instalacja","Stan","Utwórz skrót na pulpicie","Lokalizacja instalacji"),
        ["cs"] = D("Nastavení","O aplikaci","Přidat","Upravit","Odebrat","Připojit","Odpojit","Nahrát","Stáhnout","Obnovit","Nová složka","Přejmenovat","Odstranit","Zrušit","Zavřít","Pokračovat","Uložit","Instalovat","Aktualizovat","Odinstalovat","Spustit","Hostitel","Port","Zabezpečení","Uživatelské jméno","Heslo","Jazyk","Vzhled","Tmavý","Světlý","Soubory","Místní","Vzdálené","Přenosy","Uložené servery","Rychlé připojení","Instalace","Stav","Vytvořit zástupce na ploše","Umístění instalace"),
        ["sk"] = D("Nastavenia","O aplikácii","Pridať","Upraviť","Odstrániť","Pripojiť","Odpojiť","Nahrať","Stiahnuť","Obnoviť","Nový priečinok","Premenovať","Odstrániť","Zrušiť","Zavrieť","Pokračovať","Uložiť","Inštalovať","Aktualizovať","Odinštalovať","Spustiť","Hostiteľ","Port","Zabezpečenie","Používateľské meno","Heslo","Jazyk","Vzhľad","Tmavý","Svetlý","Súbory","Lokálne","Vzdialené","Prenosy","Uložené servery","Rýchle pripojenie","Inštalácia","Stav","Vytvoriť odkaz na ploche","Umiestnenie inštalácie"),
        ["sl"] = D("Nastavitve","O programu","Dodaj","Uredi","Odstrani","Poveži","Prekini povezavo","Naloži","Prenesi","Osveži","Nova mapa","Preimenuj","Izbriši","Prekliči","Zapri","Nadaljuj","Shrani","Namesti","Posodobi","Odstrani","Zaženi","Gostitelj","Vrata","Varnost","Uporabniško ime","Geslo","Jezik","Videz","Temno","Svetlo","Datoteke","Lokalno","Oddaljeno","Prenosi","Shranjeni strežniki","Hitra povezava","Namestitev","Stanje","Ustvari bližnjico na namizju","Mesto namestitve"),
        ["hu"] = D("Beállítások","Névjegy","Hozzáadás","Szerkesztés","Eltávolítás","Csatlakozás","Kapcsolat bontása","Feltöltés","Letöltés","Frissítés","Új mappa","Átnevezés","Törlés","Mégse","Bezárás","Folytatás","Mentés","Telepítés","Frissítés","Eltávolítás","Indítás","Gazdagép","Port","Biztonság","Felhasználónév","Jelszó","Nyelv","Megjelenés","Sötét","Világos","Fájlok","Helyi","Távoli","Átvitelek","Mentett szerverek","Gyors csatlakozás","Telepítő","Állapot","Asztali parancsikon létrehozása","Telepítési hely"),
        ["ro"] = D("Setări","Despre","Adaugă","Editează","Elimină","Conectare","Deconectare","Încărcare","Descărcare","Reîmprospătare","Folder nou","Redenumește","Șterge","Anulează","Închide","Continuă","Salvează","Instalează","Actualizează","Dezinstalează","Pornește","Gazdă","Port","Securitate","Utilizator","Parolă","Limbă","Aspect","Întunecat","Luminos","Fișiere","Local","La distanță","Transferuri","Servere salvate","Conectare rapidă","Instalare","Stare","Creează comandă rapidă pe desktop","Locație instalare"),
        ["bg"] = D("Настройки","Относно","Добавяне","Редактиране","Премахване","Свързване","Прекъсване","Качване","Изтегляне","Опресняване","Нова папка","Преименуване","Изтриване","Отказ","Затвори","Продължи","Запази","Инсталирай","Актуализирай","Деинсталирай","Стартирай","Хост","Порт","Сигурност","Потребител","Парола","Език","Изглед","Тъмен","Светъл","Файлове","Локално","Отдалечено","Прехвърляния","Запазени сървъри","Бързо свързване","Инсталация","Състояние","Създай пряк път на работния плот","Място за инсталация"),
        ["el"] = D("Ρυθμίσεις","Σχετικά","Προσθήκη","Επεξεργασία","Αφαίρεση","Σύνδεση","Αποσύνδεση","Μεταφόρτωση","Λήψη","Ανανέωση","Νέος φάκελος","Μετονομασία","Διαγραφή","Ακύρωση","Κλείσιμο","Συνέχεια","Αποθήκευση","Εγκατάσταση","Ενημέρωση","Απεγκατάσταση","Εκκίνηση","Κεντρικός υπολογιστής","Θύρα","Ασφάλεια","Όνομα χρήστη","Κωδικός","Γλώσσα","Εμφάνιση","Σκούρο","Φωτεινό","Αρχεία","Τοπικά","Απομακρυσμένα","Μεταφορές","Αποθηκευμένοι διακομιστές","Γρήγορη σύνδεση","Εγκατάσταση","Κατάσταση","Δημιουργία συντόμευσης επιφάνειας εργασίας","Τοποθεσία εγκατάστασης"),
        ["tr"] = D("Ayarlar","Hakkında","Ekle","Düzenle","Kaldır","Bağlan","Bağlantıyı kes","Yükle","İndir","Yenile","Yeni klasör","Yeniden adlandır","Sil","İptal","Kapat","Devam","Kaydet","Yükle","Güncelle","Kaldır","Başlat","Ana bilgisayar","Bağlantı noktası","Güvenlik","Kullanıcı adı","Parola","Dil","Görünüm","Koyu","Açık","Dosyalar","Yerel","Uzak","Aktarımlar","Kayıtlı sunucular","Hızlı bağlantı","Kurulum","Durum","Masaüstü kısayolu oluştur","Kurulum konumu"),
        ["uk"] = D("Налаштування","Про програму","Додати","Редагувати","Видалити","Підключити","Відключити","Завантажити на сервер","Завантажити","Оновити","Нова папка","Перейменувати","Видалити","Скасувати","Закрити","Продовжити","Зберегти","Встановити","Оновити","Видалити","Запустити","Хост","Порт","Безпека","Ім’я користувача","Пароль","Мова","Вигляд","Темна","Світла","Файли","Локальні","Віддалені","Передавання","Збережені сервери","Швидке підключення","Встановлення","Стан","Створити ярлик на робочому столі","Місце встановлення"),
        ["ru"] = D("Настройки","О программе","Добавить","Изменить","Удалить","Подключиться","Отключиться","Загрузить","Скачать","Обновить","Новая папка","Переименовать","Удалить","Отмена","Закрыть","Продолжить","Сохранить","Установить","Обновить","Удалить","Запустить","Хост","Порт","Безопасность","Имя пользователя","Пароль","Язык","Оформление","Тёмное","Светлое","Файлы","Локально","Удалённо","Передачи","Сохранённые серверы","Быстрое подключение","Установка","Состояние","Создать ярлык на рабочем столе","Папка установки"),
        ["sr"] = D("Podešavanja","O programu","Dodaj","Uredi","Ukloni","Poveži se","Prekini vezu","Otpremi","Preuzmi","Osveži","Nova fascikla","Preimenuj","Izbriši","Otkaži","Zatvori","Nastavi","Sačuvaj","Instaliraj","Ažuriraj","Deinstaliraj","Pokreni","Host","Port","Bezbednost","Korisničko ime","Lozinka","Jezik","Izgled","Tamno","Svetlo","Datoteke","Lokalno","Udaljeno","Prenosi","Sačuvani serveri","Brzo povezivanje","Instalacija","Status","Napravi prečicu na radnoj površini","Lokacija instalacije"),
        ["bs"] = D("Postavke","O programu","Dodaj","Uredi","Ukloni","Poveži se","Prekini vezu","Pošalji","Preuzmi","Osvježi","Nova mapa","Preimenuj","Izbriši","Odustani","Zatvori","Nastavi","Spremi","Instaliraj","Ažuriraj","Deinstaliraj","Pokreni","Host","Port","Sigurnost","Korisničko ime","Lozinka","Jezik","Izgled","Tamno","Svijetlo","Datoteke","Lokalno","Udaljeno","Prijenosi","Spremljeni serveri","Brzo povezivanje","Instalacija","Status","Napravi prečicu na radnoj površini","Lokacija instalacije"),
        ["sv"] = D("Inställningar","Om","Lägg till","Redigera","Ta bort","Anslut","Koppla från","Ladda upp","Ladda ner","Uppdatera","Ny mapp","Byt namn","Ta bort","Avbryt","Stäng","Fortsätt","Spara","Installera","Uppdatera","Avinstallera","Starta","Värd","Port","Säkerhet","Användarnamn","Lösenord","Språk","Utseende","Mörkt","Ljust","Filer","Lokalt","Fjärr","Överföringar","Sparade servrar","Snabbanslutning","Installation","Status","Skapa genväg på skrivbordet","Installationsplats"),
        ["da"] = D("Indstillinger","Om","Tilføj","Rediger","Fjern","Forbind","Afbryd","Upload","Download","Opdater","Ny mappe","Omdøb","Slet","Annuller","Luk","Fortsæt","Gem","Installer","Opdater","Afinstaller","Start","Vært","Port","Sikkerhed","Brugernavn","Adgangskode","Sprog","Udseende","Mørk","Lys","Filer","Lokal","Fjern","Overførsler","Gemte servere","Hurtig forbindelse","Installation","Status","Opret skrivebordsgenvej","Installationsplacering"),
        ["no"] = D("Innstillinger","Om","Legg til","Rediger","Fjern","Koble til","Koble fra","Last opp","Last ned","Oppdater","Ny mappe","Gi nytt navn","Slett","Avbryt","Lukk","Fortsett","Lagre","Installer","Oppdater","Avinstaller","Start","Vert","Port","Sikkerhet","Brukernavn","Passord","Språk","Utseende","Mørk","Lys","Filer","Lokalt","Eksternt","Overføringer","Lagrede servere","Hurtigkobling","Installasjon","Status","Opprett skrivebordssnarvei","Installasjonssted"),
        ["fi"] = D("Asetukset","Tietoja","Lisää","Muokkaa","Poista","Yhdistä","Katkaise yhteys","Lähetä","Lataa","Päivitä","Uusi kansio","Nimeä uudelleen","Poista","Peruuta","Sulje","Jatka","Tallenna","Asenna","Päivitä","Poista asennus","Käynnistä","Palvelin","Portti","Suojaus","Käyttäjänimi","Salasana","Kieli","Ulkoasu","Tumma","Vaalea","Tiedostot","Paikallinen","Etä","Siirrot","Tallennetut palvelimet","Pikayhteys","Asennus","Tila","Luo työpöydän pikakuvake","Asennussijainti"),
        ["ja"] = D("設定","情報","追加","編集","削除","接続","切断","アップロード","ダウンロード","更新","新しいフォルダー","名前の変更","削除","キャンセル","閉じる","続行","保存","インストール","更新","アンインストール","起動","ホスト","ポート","セキュリティ","ユーザー名","パスワード","言語","外観","ダーク","ライト","ファイル","ローカル","リモート","転送","保存済みサーバー","クイック接続","セットアップ","状態","デスクトップショートカットを作成","インストール先"),
        ["ko"] = D("설정","정보","추가","편집","제거","연결","연결 끊기","업로드","다운로드","새로 고침","새 폴더","이름 바꾸기","삭제","취소","닫기","계속","저장","설치","업데이트","제거","실행","호스트","포트","보안","사용자 이름","비밀번호","언어","모양","어둡게","밝게","파일","로컬","원격","전송","저장된 서버","빠른 연결","설치","상태","바탕 화면 바로 가기 만들기","설치 위치"),
        ["zh-CN"] = D("设置","关于","添加","编辑","移除","连接","断开连接","上传","下载","刷新","新建文件夹","重命名","删除","取消","关闭","继续","保存","安装","更新","卸载","启动","主机","端口","安全","用户名","密码","语言","外观","深色","浅色","文件","本地","远程","传输","已保存的服务器","快速连接","安装程序","状态","创建桌面快捷方式","安装位置"),
        ["zh-TW"] = D("設定","關於","新增","編輯","移除","連線","中斷連線","上傳","下載","重新整理","新增資料夾","重新命名","刪除","取消","關閉","繼續","儲存","安裝","更新","解除安裝","啟動","主機","連接埠","安全性","使用者名稱","密碼","語言","外觀","深色","淺色","檔案","本機","遠端","傳輸","已儲存的伺服器","快速連線","安裝程式","狀態","建立桌面捷徑","安裝位置")
    };

    private static string _currentLanguageCode = DefaultLanguageCode;

    public static IReadOnlyList<GhostLanguage> SupportedLanguages => LanguageList;
    public static IReadOnlyList<string> RequiredCoreTranslationKeys => CoreTranslationKeys;
    public static string CurrentLanguageCode => _currentLanguageCode;

    public static string NormalizeLanguageCode(string? code)
    {
        if (string.IsNullOrWhiteSpace(code))
            return DefaultLanguageCode;

        var exact = LanguageList.FirstOrDefault(x => string.Equals(x.Code, code.Trim(), StringComparison.OrdinalIgnoreCase));
        return exact?.Code ?? DefaultLanguageCode;
    }

    public static void SetLanguage(string? code)
    {
        _currentLanguageCode = NormalizeLanguageCode(code);
    }

    public static string T(string key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        if (_currentLanguageCode != DefaultLanguageCode &&
            Overrides.TryGetValue(_currentLanguageCode, out var locale) &&
            locale.TryGetValue(key, out var translated) &&
            !string.IsNullOrWhiteSpace(translated))
        {
            return translated;
        }

        return English.TryGetValue(key, out var fallback) ? fallback : key;
    }

    public static string F(string key, params object?[] args) =>
        string.Format(CultureInfo.CurrentCulture, T(key), args);

    public static bool HasCoreCoverage(string languageCode)
    {
        languageCode = NormalizeLanguageCode(languageCode);
        if (languageCode == DefaultLanguageCode)
            return CoreTranslationKeys.All(English.ContainsKey);
        return Overrides.TryGetValue(languageCode, out var locale) &&
               CoreTranslationKeys.All(key => locale.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value));
    }

    private static Dictionary<string, string> D(params string[] values)
    {
        var result = new Dictionary<string, string>(StringComparer.Ordinal);
        var count = Math.Min(values.Length, CoreTranslationKeys.Length);
        for (var i = 0; i < count; i++)
            result[CoreTranslationKeys[i]] = values[i];
        return result;
    }
}
