using System.Globalization;

namespace GhostFTP.Design;

public sealed record GhostLanguage(string Code, string NativeName, string EnglishName)
{
    public override string ToString() => $"{NativeName} — {EnglishName}";
}

public static class GhostLocalization
{
    public const string DefaultLanguageCode = "en";

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
        ["EnglishFallback"] = "English is the primary language and fallback for any untranslated technical text.",
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
        ["hr"] = D("Postavke","O aplikaciji","Dodaj","Uredi","Ukloni","Poveži se","Prekini vezu","Poveži odabrano","Prenesi na server","Preuzmi","Osvježi","Nova mapa","Preimenuj","Izbriši","Odustani","Zatvori","Nastavi","Spremi","Spremi server","Spremi postavke","Instaliraj","Ažuriraj","Deinstaliraj","Pokreni","Poslužitelj","Port","Sigurnost","Korisničko ime","Lozinka","Jezik","Izgled","Tamno","Svijetlo","Koristi postavku sustava Windows","Datoteke","Lokalno","Udaljeno","Prijenosi","Spremljeni serveri","Brzo povezivanje","Instalacija","Status","Privatnost po dizajnu","Bez telemetrije · Bez praćenja","Stvori prečac na radnoj površini","Ukloni i lokalne postavke i spremljene profile servera","Mjesto instalacije","Povezani server","Ovo računalo"),
        ["de"] = D("Einstellungen","Info","Hinzufügen","Bearbeiten","Entfernen","Verbinden","Trennen","Auswahl verbinden","Hochladen","Herunterladen","Aktualisieren","Neuer Ordner","Umbenennen","Löschen","Abbrechen","Schließen","Weiter","Speichern","Server speichern","Einstellungen speichern","Installieren","Aktualisieren","Deinstallieren","Starten","Host","Port","Sicherheit","Benutzername","Passwort","Sprache","Darstellung","Dunkel","Hell","Windows-Einstellung verwenden","Dateien","Lokal","Remote","Übertragungen","Gespeicherte Server","Schnell verbinden","Setup","Status","Datenschutz durch Design","Keine Telemetrie · Kein Tracking","Desktop-Verknüpfung erstellen","Lokale Einstellungen und gespeicherte Serverprofile ebenfalls entfernen","Installationsort","Verbundener Server","Dieser PC"),
        ["fr"] = D("Paramètres","À propos","Ajouter","Modifier","Supprimer","Se connecter","Déconnecter","Connecter la sélection","Envoyer","Télécharger","Actualiser","Nouveau dossier","Renommer","Supprimer","Annuler","Fermer","Continuer","Enregistrer","Enregistrer le serveur","Enregistrer les paramètres","Installer","Mettre à jour","Désinstaller","Lancer","Hôte","Port","Sécurité","Nom d’utilisateur","Mot de passe","Langue","Apparence","Sombre","Clair","Utiliser le paramètre Windows","Fichiers","Local","Distant","Transferts","Serveurs enregistrés","Connexion rapide","Installation","État","Confidentialité dès la conception","Aucune télémétrie · Aucun suivi","Créer un raccourci sur le bureau","Supprimer aussi les paramètres locaux et les profils de serveur enregistrés","Emplacement d’installation","Serveur connecté","Ce PC"),
        ["es"] = D("Configuración","Acerca de","Añadir","Editar","Quitar","Conectar","Desconectar","Conectar selección","Subir","Descargar","Actualizar","Nueva carpeta","Renombrar","Eliminar","Cancelar","Cerrar","Continuar","Guardar","Guardar servidor","Guardar configuración","Instalar","Actualizar","Desinstalar","Iniciar","Host","Puerto","Seguridad","Usuario","Contraseña","Idioma","Apariencia","Oscuro","Claro","Usar configuración de Windows","Archivos","Local","Remoto","Transferencias","Servidores guardados","Conexión rápida","Instalación","Estado","Privacidad por diseño","Sin telemetría · Sin seguimiento","Crear acceso directo en el escritorio","Eliminar también la configuración local y los perfiles de servidor guardados","Ubicación de instalación","Servidor conectado","Este PC"),
        ["it"] = D("Impostazioni","Informazioni","Aggiungi","Modifica","Rimuovi","Connetti","Disconnetti","Connetti selezionato","Carica","Scarica","Aggiorna","Nuova cartella","Rinomina","Elimina","Annulla","Chiudi","Continua","Salva","Salva server","Salva impostazioni","Installa","Aggiorna","Disinstalla","Avvia","Host","Porta","Sicurezza","Nome utente","Password","Lingua","Aspetto","Scuro","Chiaro","Usa impostazione Windows","File","Locale","Remoto","Trasferimenti","Server salvati","Connessione rapida","Installazione","Stato","Privacy by design","Nessuna telemetria · Nessun tracciamento","Crea collegamento sul desktop","Rimuovi anche impostazioni locali e profili server salvati","Percorso di installazione","Server connesso","Questo PC"),
        ["pt"] = D("Definições","Sobre","Adicionar","Editar","Remover","Ligar","Desligar","Ligar selecionado","Enviar","Transferir","Atualizar","Nova pasta","Renomear","Eliminar","Cancelar","Fechar","Continuar","Guardar","Guardar servidor","Guardar definições","Instalar","Atualizar","Desinstalar","Iniciar","Host","Porta","Segurança","Utilizador","Palavra-passe","Idioma","Aspeto","Escuro","Claro","Usar definição do Windows","Ficheiros","Local","Remoto","Transferências","Servidores guardados","Ligação rápida","Instalação","Estado","Privacidade desde a conceção","Sem telemetria · Sem rastreio","Criar atalho no ambiente de trabalho","Remover também definições locais e perfis de servidor guardados","Local de instalação","Servidor ligado","Este PC"),
        ["nl"] = D("Instellingen","Over","Toevoegen","Bewerken","Verwijderen","Verbinden","Verbinding verbreken","Selectie verbinden","Uploaden","Downloaden","Vernieuwen","Nieuwe map","Hernoemen","Verwijderen","Annuleren","Sluiten","Doorgaan","Opslaan","Server opslaan","Instellingen opslaan","Installeren","Bijwerken","Verwijderen","Starten","Host","Poort","Beveiliging","Gebruikersnaam","Wachtwoord","Taal","Weergave","Donker","Licht","Windows-instelling gebruiken","Bestanden","Lokaal","Extern","Overdrachten","Opgeslagen servers","Snel verbinden","Installatie","Status","Privacy by design","Geen telemetrie · Geen tracking","Bureaubladsnelkoppeling maken","Ook lokale instellingen en opgeslagen serverprofielen verwijderen","Installatielocatie","Verbonden server","Deze pc"),
        ["pl"] = D("Ustawienia","O programie","Dodaj","Edytuj","Usuń","Połącz","Rozłącz","Połącz wybrany","Wyślij","Pobierz","Odśwież","Nowy folder","Zmień nazwę","Usuń","Anuluj","Zamknij","Kontynuuj","Zapisz","Zapisz serwer","Zapisz ustawienia","Zainstaluj","Aktualizuj","Odinstaluj","Uruchom","Host","Port","Bezpieczeństwo","Nazwa użytkownika","Hasło","Język","Wygląd","Ciemny","Jasny","Użyj ustawienia Windows","Pliki","Lokalne","Zdalne","Transfery","Zapisane serwery","Szybkie połączenie","Instalacja","Stan","Prywatność od podstaw","Bez telemetrii · Bez śledzenia","Utwórz skrót na pulpicie","Usuń także ustawienia lokalne i zapisane profile serwerów","Lokalizacja instalacji","Połączony serwer","Ten komputer"),
        ["cs"] = D("Nastavení","O aplikaci","Přidat","Upravit","Odebrat","Připojit","Odpojit","Připojit vybraný","Nahrát","Stáhnout","Obnovit","Nová složka","Přejmenovat","Odstranit","Zrušit","Zavřít","Pokračovat","Uložit","Uložit server","Uložit nastavení","Instalovat","Aktualizovat","Odinstalovat","Spustit","Hostitel","Port","Zabezpečení","Uživatelské jméno","Heslo","Jazyk","Vzhled","Tmavý","Světlý","Použít nastavení Windows","Soubory","Místní","Vzdálené","Přenosy","Uložené servery","Rychlé připojení","Instalace","Stav","Soukromí od návrhu","Bez telemetrie · Bez sledování","Vytvořit zástupce na ploše","Odstranit také místní nastavení a uložené profily serverů","Umístění instalace","Připojený server","Tento počítač"),
        ["sk"] = D("Nastavenia","O aplikácii","Pridať","Upraviť","Odstrániť","Pripojiť","Odpojiť","Pripojiť vybraný","Nahrať","Stiahnuť","Obnoviť","Nový priečinok","Premenovať","Odstrániť","Zrušiť","Zavrieť","Pokračovať","Uložiť","Uložiť server","Uložiť nastavenia","Inštalovať","Aktualizovať","Odinštalovať","Spustiť","Hostiteľ","Port","Zabezpečenie","Používateľské meno","Heslo","Jazyk","Vzhľad","Tmavý","Svetlý","Použiť nastavenie Windows","Súbory","Lokálne","Vzdialené","Prenosy","Uložené servery","Rýchle pripojenie","Inštalácia","Stav","Súkromie od návrhu","Bez telemetrie · Bez sledovania","Vytvoriť odkaz na ploche","Odstrániť aj lokálne nastavenia a uložené profily serverov","Umiestnenie inštalácie","Pripojený server","Tento počítač"),
        ["sl"] = D("Nastavitve","O programu","Dodaj","Uredi","Odstrani","Poveži","Prekini povezavo","Poveži izbrano","Naloži","Prenesi","Osveži","Nova mapa","Preimenuj","Izbriši","Prekliči","Zapri","Nadaljuj","Shrani","Shrani strežnik","Shrani nastavitve","Namesti","Posodobi","Odstrani","Zaženi","Gostitelj","Vrata","Varnost","Uporabniško ime","Geslo","Jezik","Videz","Temno","Svetlo","Uporabi nastavitev Windows","Datoteke","Lokalno","Oddaljeno","Prenosi","Shranjeni strežniki","Hitra povezava","Namestitev","Stanje","Zasebnost po zasnovi","Brez telemetrije · Brez sledenja","Ustvari bližnjico na namizju","Odstrani tudi lokalne nastavitve in shranjene profile strežnikov","Mesto namestitve","Povezan strežnik","Ta računalnik"),
        ["hu"] = D("Beállítások","Névjegy","Hozzáadás","Szerkesztés","Eltávolítás","Csatlakozás","Kapcsolat bontása","Kijelölt csatlakoztatása","Feltöltés","Letöltés","Frissítés","Új mappa","Átnevezés","Törlés","Mégse","Bezárás","Folytatás","Mentés","Szerver mentése","Beállítások mentése","Telepítés","Frissítés","Eltávolítás","Indítás","Gazdagép","Port","Biztonság","Felhasználónév","Jelszó","Nyelv","Megjelenés","Sötét","Világos","Windows-beállítás használata","Fájlok","Helyi","Távoli","Átvitelek","Mentett szerverek","Gyors csatlakozás","Telepítő","Állapot","Adatvédelem alapból","Nincs telemetria · Nincs követés","Asztali parancsikon létrehozása","Helyi beállítások és mentett szerverprofilok eltávolítása is","Telepítési hely","Csatlakoztatott szerver","Ez a gép"),
        ["ro"] = D("Setări","Despre","Adaugă","Editează","Elimină","Conectare","Deconectare","Conectează selecția","Încărcare","Descărcare","Reîmprospătare","Folder nou","Redenumește","Șterge","Anulează","Închide","Continuă","Salvează","Salvează serverul","Salvează setările","Instalează","Actualizează","Dezinstalează","Pornește","Gazdă","Port","Securitate","Utilizator","Parolă","Limbă","Aspect","Întunecat","Luminos","Folosește setarea Windows","Fișiere","Local","La distanță","Transferuri","Servere salvate","Conectare rapidă","Instalare","Stare","Confidențialitate prin design","Fără telemetrie · Fără urmărire","Creează comandă rapidă pe desktop","Elimină și setările locale și profilurile de server salvate","Locație instalare","Server conectat","Acest PC"),
        ["bg"] = D("Настройки","Относно","Добавяне","Редактиране","Премахване","Свързване","Прекъсване","Свържи избраното","Качване","Изтегляне","Опресняване","Нова папка","Преименуване","Изтриване","Отказ","Затвори","Продължи","Запази","Запази сървър","Запази настройките","Инсталирай","Актуализирай","Деинсталирай","Стартирай","Хост","Порт","Сигурност","Потребител","Парола","Език","Изглед","Тъмен","Светъл","Използвай настройката на Windows","Файлове","Локално","Отдалечено","Прехвърляния","Запазени сървъри","Бързо свързване","Инсталация","Състояние","Поверителност по дизайн","Без телеметрия · Без проследяване","Създай пряк път на работния плот","Премахни и локалните настройки и запазените профили","Място за инсталация","Свързан сървър","Този компютър"),
        ["el"] = D("Ρυθμίσεις","Σχετικά","Προσθήκη","Επεξεργασία","Αφαίρεση","Σύνδεση","Αποσύνδεση","Σύνδεση επιλεγμένου","Μεταφόρτωση","Λήψη","Ανανέωση","Νέος φάκελος","Μετονομασία","Διαγραφή","Ακύρωση","Κλείσιμο","Συνέχεια","Αποθήκευση","Αποθήκευση διακομιστή","Αποθήκευση ρυθμίσεων","Εγκατάσταση","Ενημέρωση","Απεγκατάσταση","Εκκίνηση","Κεντρικός υπολογιστής","Θύρα","Ασφάλεια","Όνομα χρήστη","Κωδικός","Γλώσσα","Εμφάνιση","Σκούρο","Φωτεινό","Χρήση ρύθμισης Windows","Αρχεία","Τοπικά","Απομακρυσμένα","Μεταφορές","Αποθηκευμένοι διακομιστές","Γρήγορη σύνδεση","Εγκατάσταση","Κατάσταση","Ιδιωτικότητα από σχεδιασμό","Χωρίς τηλεμετρία · Χωρίς παρακολούθηση","Δημιουργία συντόμευσης επιφάνειας εργασίας","Αφαίρεση και τοπικών ρυθμίσεων και αποθηκευμένων προφίλ","Τοποθεσία εγκατάστασης","Συνδεδεμένος διακομιστής","Αυτός ο υπολογιστής"),
        ["tr"] = D("Ayarlar","Hakkında","Ekle","Düzenle","Kaldır","Bağlan","Bağlantıyı kes","Seçilene bağlan","Yükle","İndir","Yenile","Yeni klasör","Yeniden adlandır","Sil","İptal","Kapat","Devam","Kaydet","Sunucuyu kaydet","Ayarları kaydet","Yükle","Güncelle","Kaldır","Başlat","Ana bilgisayar","Bağlantı noktası","Güvenlik","Kullanıcı adı","Parola","Dil","Görünüm","Koyu","Açık","Windows ayarını kullan","Dosyalar","Yerel","Uzak","Aktarımlar","Kayıtlı sunucular","Hızlı bağlantı","Kurulum","Durum","Tasarım gereği gizlilik","Telemetri yok · İzleme yok","Masaüstü kısayolu oluştur","Yerel ayarları ve kayıtlı sunucu profillerini de kaldır","Kurulum konumu","Bağlı sunucu","Bu bilgisayar"),
        ["uk"] = D("Налаштування","Про програму","Додати","Редагувати","Видалити","Підключити","Відключити","Підключити вибране","Завантажити на сервер","Завантажити","Оновити","Нова папка","Перейменувати","Видалити","Скасувати","Закрити","Продовжити","Зберегти","Зберегти сервер","Зберегти налаштування","Встановити","Оновити","Видалити","Запустити","Хост","Порт","Безпека","Ім’я користувача","Пароль","Мова","Вигляд","Темна","Світла","Використовувати налаштування Windows","Файли","Локальні","Віддалені","Передавання","Збережені сервери","Швидке підключення","Встановлення","Стан","Конфіденційність за задумом","Без телеметрії · Без відстеження","Створити ярлик на робочому столі","Також видалити локальні налаштування та збережені профілі серверів","Місце встановлення","Підключений сервер","Цей ПК"),
        ["ru"] = D("Настройки","О программе","Добавить","Изменить","Удалить","Подключиться","Отключиться","Подключить выбранное","Загрузить","Скачать","Обновить","Новая папка","Переименовать","Удалить","Отмена","Закрыть","Продолжить","Сохранить","Сохранить сервер","Сохранить настройки","Установить","Обновить","Удалить","Запустить","Хост","Порт","Безопасность","Имя пользователя","Пароль","Язык","Оформление","Тёмное","Светлое","Использовать настройку Windows","Файлы","Локально","Удалённо","Передачи","Сохранённые серверы","Быстрое подключение","Установка","Состояние","Конфиденциальность по замыслу","Без телеметрии · Без отслеживания","Создать ярлык на рабочем столе","Также удалить локальные настройки и сохранённые профили серверов","Папка установки","Подключённый сервер","Этот компьютер"),
        ["sr"] = D("Podešavanja","O programu","Dodaj","Uredi","Ukloni","Poveži se","Prekini vezu","Poveži izabrano","Otpremi","Preuzmi","Osveži","Nova fascikla","Preimenuj","Izbriši","Otkaži","Zatvori","Nastavi","Sačuvaj","Sačuvaj server","Sačuvaj podešavanja","Instaliraj","Ažuriraj","Deinstaliraj","Pokreni","Host","Port","Bezbednost","Korisničko ime","Lozinka","Jezik","Izgled","Tamno","Svetlo","Koristi Windows podešavanje","Datoteke","Lokalno","Udaljeno","Prenosi","Sačuvani serveri","Brzo povezivanje","Instalacija","Status","Privatnost po dizajnu","Bez telemetrije · Bez praćenja","Napravi prečicu na radnoj površini","Ukloni i lokalna podešavanja i sačuvane profile servera","Lokacija instalacije","Povezani server","Ovaj računar"),
        ["bs"] = D("Postavke","O programu","Dodaj","Uredi","Ukloni","Poveži se","Prekini vezu","Poveži odabrano","Pošalji","Preuzmi","Osvježi","Nova mapa","Preimenuj","Izbriši","Odustani","Zatvori","Nastavi","Spremi","Spremi server","Spremi postavke","Instaliraj","Ažuriraj","Deinstaliraj","Pokreni","Host","Port","Sigurnost","Korisničko ime","Lozinka","Jezik","Izgled","Tamno","Svijetlo","Koristi Windows postavku","Datoteke","Lokalno","Udaljeno","Prijenosi","Spremljeni serveri","Brzo povezivanje","Instalacija","Status","Privatnost po dizajnu","Bez telemetrije · Bez praćenja","Napravi prečicu na radnoj površini","Ukloni i lokalne postavke i spremljene profile servera","Lokacija instalacije","Povezani server","Ovaj računar"),
        ["sv"] = D("Inställningar","Om","Lägg till","Redigera","Ta bort","Anslut","Koppla från","Anslut markerad","Ladda upp","Ladda ner","Uppdatera","Ny mapp","Byt namn","Ta bort","Avbryt","Stäng","Fortsätt","Spara","Spara server","Spara inställningar","Installera","Uppdatera","Avinstallera","Starta","Värd","Port","Säkerhet","Användarnamn","Lösenord","Språk","Utseende","Mörkt","Ljust","Använd Windows-inställning","Filer","Lokalt","Fjärr","Överföringar","Sparade servrar","Snabbanslutning","Installation","Status","Integritet från grunden","Ingen telemetri · Ingen spårning","Skapa genväg på skrivbordet","Ta även bort lokala inställningar och sparade serverprofiler","Installationsplats","Ansluten server","Den här datorn"),
        ["da"] = D("Indstillinger","Om","Tilføj","Rediger","Fjern","Forbind","Afbryd","Forbind valgte","Upload","Download","Opdater","Ny mappe","Omdøb","Slet","Annuller","Luk","Fortsæt","Gem","Gem server","Gem indstillinger","Installer","Opdater","Afinstaller","Start","Vært","Port","Sikkerhed","Brugernavn","Adgangskode","Sprog","Udseende","Mørk","Lys","Brug Windows-indstilling","Filer","Lokal","Fjern","Overførsler","Gemte servere","Hurtig forbindelse","Installation","Status","Privatliv fra design","Ingen telemetri · Ingen sporing","Opret skrivebordsgenvej","Fjern også lokale indstillinger og gemte serverprofiler","Installationsplacering","Forbundet server","Denne pc"),
        ["no"] = D("Innstillinger","Om","Legg til","Rediger","Fjern","Koble til","Koble fra","Koble til valgt","Last opp","Last ned","Oppdater","Ny mappe","Gi nytt navn","Slett","Avbryt","Lukk","Fortsett","Lagre","Lagre server","Lagre innstillinger","Installer","Oppdater","Avinstaller","Start","Vert","Port","Sikkerhet","Brukernavn","Passord","Språk","Utseende","Mørk","Lys","Bruk Windows-innstilling","Filer","Lokalt","Eksternt","Overføringer","Lagrede servere","Hurtigkobling","Installasjon","Status","Personvern fra grunnen","Ingen telemetri · Ingen sporing","Opprett skrivebordssnarvei","Fjern også lokale innstillinger og lagrede serverprofiler","Installasjonssted","Tilkoblet server","Denne PC-en"),
        ["fi"] = D("Asetukset","Tietoja","Lisää","Muokkaa","Poista","Yhdistä","Katkaise yhteys","Yhdistä valittu","Lähetä","Lataa","Päivitä","Uusi kansio","Nimeä uudelleen","Poista","Peruuta","Sulje","Jatka","Tallenna","Tallenna palvelin","Tallenna asetukset","Asenna","Päivitä","Poista asennus","Käynnistä","Palvelin","Portti","Suojaus","Käyttäjänimi","Salasana","Kieli","Ulkoasu","Tumma","Vaalea","Käytä Windows-asetusta","Tiedostot","Paikallinen","Etä","Siirrot","Tallennetut palvelimet","Pikayhteys","Asennus","Tila","Tietosuoja suunnittelusta lähtien","Ei telemetriaa · Ei seurantaa","Luo työpöydän pikakuvake","Poista myös paikalliset asetukset ja tallennetut palvelinprofiilit","Asennussijainti","Yhdistetty palvelin","Tämä tietokone"),
        ["ja"] = D("設定","情報","追加","編集","削除","接続","切断","選択項目に接続","アップロード","ダウンロード","更新","新しいフォルダー","名前の変更","削除","キャンセル","閉じる","続行","保存","サーバーを保存","設定を保存","インストール","更新","アンインストール","起動","ホスト","ポート","セキュリティ","ユーザー名","パスワード","言語","外観","ダーク","ライト","Windows の設定を使用","ファイル","ローカル","リモート","転送","保存済みサーバー","クイック接続","セットアップ","状態","プライバシー重視設計","テレメトリなし · 追跡なし","デスクトップショートカットを作成","ローカル設定と保存済みサーバープロファイルも削除","インストール先","接続中のサーバー","この PC"),
        ["ko"] = D("설정","정보","추가","편집","제거","연결","연결 끊기","선택 항목 연결","업로드","다운로드","새로 고침","새 폴더","이름 바꾸기","삭제","취소","닫기","계속","저장","서버 저장","설정 저장","설치","업데이트","제거","실행","호스트","포트","보안","사용자 이름","비밀번호","언어","모양","어둡게","밝게","Windows 설정 사용","파일","로컬","원격","전송","저장된 서버","빠른 연결","설치","상태","개인정보 보호 설계","원격 분석 없음 · 추적 없음","바탕 화면 바로 가기 만들기","로컬 설정과 저장된 서버 프로필도 제거","설치 위치","연결된 서버","이 PC"),
        ["zh-CN"] = D("设置","关于","添加","编辑","移除","连接","断开连接","连接所选项","上传","下载","刷新","新建文件夹","重命名","删除","取消","关闭","继续","保存","保存服务器","保存设置","安装","更新","卸载","启动","主机","端口","安全","用户名","密码","语言","外观","深色","浅色","使用 Windows 设置","文件","本地","远程","传输","已保存的服务器","快速连接","安装程序","状态","隐私设计","无遥测 · 无跟踪","创建桌面快捷方式","同时删除本地设置和已保存的服务器配置","安装位置","已连接服务器","此电脑"),
        ["zh-TW"] = D("設定","關於","新增","編輯","移除","連線","中斷連線","連線所選項目","上傳","下載","重新整理","新增資料夾","重新命名","刪除","取消","關閉","繼續","儲存","儲存伺服器","儲存設定","安裝","更新","解除安裝","啟動","主機","連接埠","安全性","使用者名稱","密碼","語言","外觀","深色","淺色","使用 Windows 設定","檔案","本機","遠端","傳輸","已儲存的伺服器","快速連線","安裝程式","狀態","隱私優先設計","無遙測 · 無追蹤","建立桌面捷徑","同時移除本機設定與已儲存的伺服器設定檔","安裝位置","已連線伺服器","此電腦")
    };

    private static readonly string[] CoreTranslationKeys =
    [
        "Settings","About","Add","Edit","Remove","Connect","Disconnect","ConnectSelected","Upload","Download",
        "Refresh","NewFolder","Rename","Delete","Cancel","Close","Continue","Save","SaveServer","SaveSettings",
        "Install","Update","Uninstall","Launch","Host","Port","Security","Username","Password","Language",
        "Appearance","Dark","Light","UseWindowsSetting","Files","Local","Remote","Transfers","SavedServers","QuickConnect",
        "Setup","Status","PrivacyByDesign","NoTelemetryTracking","CreateDesktopShortcut","RemoveLocalData","InstallLocation","ConnectedServer","ThisPc"
    ];

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
        return Overrides.TryGetValue(languageCode, out var locale) && CoreTranslationKeys.All(locale.ContainsKey);
    }

    private static Dictionary<string, string> D(params string[] values)
    {
        if (values.Length != CoreTranslationKeys.Length)
            throw new InvalidOperationException($"Ghost FTP localization entry has {values.Length} values but {CoreTranslationKeys.Length} are required.");

        var result = new Dictionary<string, string>(StringComparer.Ordinal);
        for (var i = 0; i < CoreTranslationKeys.Length; i++)
            result[CoreTranslationKeys[i]] = values[i];
        return result;
    }
}
