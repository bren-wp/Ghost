namespace GhostFTP.Design;

public static class GhostSetupLocalization
{
    private static readonly string[] Keys =
    [
        "Welcome", "LicenseAgreement", "AcceptLicenseTerms", "Back", "Next",
        "InstallOptions", "ReadyToInstall", "Finish", "ClientLanguage", "ChooseLanguage"
    ];

    private static readonly Dictionary<string, string[]> Values = new(StringComparer.OrdinalIgnoreCase)
    {
        ["en"] = ["Welcome", "License agreement", "I accept the license terms", "Back", "Next", "Install options", "Ready to install", "Finish", "Client language", "Choose the language used by Setup and Ghost FTP."],
        ["hr"] = ["Dobro došli", "Licencni ugovor", "Prihvaćam uvjete licence", "Natrag", "Dalje", "Mogućnosti instalacije", "Spremno za instalaciju", "Završi", "Jezik klijenta", "Odaberite jezik koji će koristiti instalacija i Ghost FTP."],
        ["de"] = ["Willkommen", "Lizenzvereinbarung", "Ich akzeptiere die Lizenzbedingungen", "Zurück", "Weiter", "Installationsoptionen", "Bereit zur Installation", "Fertigstellen", "Client-Sprache", "Wählen Sie die Sprache für Setup und Ghost FTP."],
        ["fr"] = ["Bienvenue", "Contrat de licence", "J’accepte les conditions de licence", "Retour", "Suivant", "Options d’installation", "Prêt à installer", "Terminer", "Langue du client", "Choisissez la langue utilisée par l’installation et Ghost FTP."],
        ["es"] = ["Bienvenido", "Acuerdo de licencia", "Acepto los términos de la licencia", "Atrás", "Siguiente", "Opciones de instalación", "Listo para instalar", "Finalizar", "Idioma del cliente", "Elija el idioma utilizado por el instalador y Ghost FTP."],
        ["it"] = ["Benvenuto", "Contratto di licenza", "Accetto i termini della licenza", "Indietro", "Avanti", "Opzioni di installazione", "Pronto per l’installazione", "Fine", "Lingua del client", "Scegli la lingua usata dal programma di installazione e da Ghost FTP."],
        ["pt"] = ["Bem-vindo", "Contrato de licença", "Aceito os termos da licença", "Voltar", "Seguinte", "Opções de instalação", "Pronto para instalar", "Concluir", "Idioma do cliente", "Escolha o idioma usado pela instalação e pelo Ghost FTP."],
        ["nl"] = ["Welkom", "Licentieovereenkomst", "Ik accepteer de licentievoorwaarden", "Terug", "Volgende", "Installatieopties", "Klaar om te installeren", "Voltooien", "Clienttaal", "Kies de taal voor Setup en Ghost FTP."],
        ["pl"] = ["Witamy", "Umowa licencyjna", "Akceptuję warunki licencji", "Wstecz", "Dalej", "Opcje instalacji", "Gotowe do instalacji", "Zakończ", "Język klienta", "Wybierz język używany przez instalator i Ghost FTP."],
        ["cs"] = ["Vítejte", "Licenční smlouva", "Souhlasím s licenčními podmínkami", "Zpět", "Další", "Možnosti instalace", "Připraveno k instalaci", "Dokončit", "Jazyk klienta", "Zvolte jazyk používaný instalačním programem a Ghost FTP."],
        ["sk"] = ["Vitajte", "Licenčná zmluva", "Súhlasím s licenčnými podmienkami", "Späť", "Ďalej", "Možnosti inštalácie", "Pripravené na inštaláciu", "Dokončiť", "Jazyk klienta", "Vyberte jazyk používaný inštaláciou a Ghost FTP."],
        ["sl"] = ["Dobrodošli", "Licenčna pogodba", "Sprejemam licenčne pogoje", "Nazaj", "Naprej", "Možnosti namestitve", "Pripravljeno za namestitev", "Dokončaj", "Jezik odjemalca", "Izberite jezik, ki ga uporabljata namestitev in Ghost FTP."],
        ["hu"] = ["Üdvözöljük", "Licencszerződés", "Elfogadom a licencfeltételeket", "Vissza", "Tovább", "Telepítési beállítások", "Telepítésre kész", "Befejezés", "Kliens nyelve", "Válassza ki a telepítő és a Ghost FTP nyelvét."],
        ["ro"] = ["Bun venit", "Acord de licență", "Accept termenii licenței", "Înapoi", "Următorul", "Opțiuni de instalare", "Pregătit pentru instalare", "Finalizare", "Limba clientului", "Alegeți limba folosită de instalare și Ghost FTP."],
        ["bg"] = ["Добре дошли", "Лицензионно споразумение", "Приемам условията на лиценза", "Назад", "Напред", "Опции за инсталиране", "Готово за инсталиране", "Готово", "Език на клиента", "Изберете езика за инсталацията и Ghost FTP."],
        ["el"] = ["Καλώς ορίσατε", "Άδεια χρήσης", "Αποδέχομαι τους όρους της άδειας", "Πίσω", "Επόμενο", "Επιλογές εγκατάστασης", "Έτοιμο για εγκατάσταση", "Τέλος", "Γλώσσα εφαρμογής", "Επιλέξτε τη γλώσσα για την εγκατάσταση και το Ghost FTP."],
        ["tr"] = ["Hoş geldiniz", "Lisans sözleşmesi", "Lisans koşullarını kabul ediyorum", "Geri", "İleri", "Kurulum seçenekleri", "Kuruluma hazır", "Bitir", "İstemci dili", "Kurulum ve Ghost FTP tarafından kullanılacak dili seçin."],
        ["uk"] = ["Ласкаво просимо", "Ліцензійна угода", "Я приймаю умови ліцензії", "Назад", "Далі", "Параметри встановлення", "Готово до встановлення", "Завершити", "Мова клієнта", "Виберіть мову для інсталятора та Ghost FTP."],
        ["ru"] = ["Добро пожаловать", "Лицензионное соглашение", "Я принимаю условия лицензии", "Назад", "Далее", "Параметры установки", "Готово к установке", "Готово", "Язык клиента", "Выберите язык установщика и Ghost FTP."],
        ["sr"] = ["Dobro došli", "Licencni ugovor", "Prihvatam uslove licence", "Nazad", "Dalje", "Opcije instalacije", "Spremno za instalaciju", "Završi", "Jezik klijenta", "Izaberite jezik koji koriste instalacija i Ghost FTP."],
        ["bs"] = ["Dobro došli", "Licencni ugovor", "Prihvatam uslove licence", "Nazad", "Dalje", "Opcije instalacije", "Spremno za instalaciju", "Završi", "Jezik klijenta", "Odaberite jezik koji koriste instalacija i Ghost FTP."],
        ["sv"] = ["Välkommen", "Licensavtal", "Jag accepterar licensvillkoren", "Tillbaka", "Nästa", "Installationsalternativ", "Redo att installera", "Slutför", "Klientspråk", "Välj språket som används av installationen och Ghost FTP."],
        ["da"] = ["Velkommen", "Licensaftale", "Jeg accepterer licensvilkårene", "Tilbage", "Næste", "Installationsindstillinger", "Klar til installation", "Afslut", "Klientsprog", "Vælg sproget til installationen og Ghost FTP."],
        ["no"] = ["Velkommen", "Lisensavtale", "Jeg godtar lisensvilkårene", "Tilbake", "Neste", "Installasjonsalternativer", "Klar til installasjon", "Fullfør", "Klientspråk", "Velg språket som brukes av installasjonen og Ghost FTP."],
        ["fi"] = ["Tervetuloa", "Käyttöoikeussopimus", "Hyväksyn käyttöoikeusehdot", "Takaisin", "Seuraava", "Asennusvalinnat", "Valmis asennettavaksi", "Valmis", "Sovelluksen kieli", "Valitse asennuksen ja Ghost FTP:n käyttämä kieli."],
        ["ja"] = ["ようこそ", "使用許諾契約", "ライセンス条項に同意します", "戻る", "次へ", "インストール オプション", "インストールの準備完了", "完了", "クライアント言語", "セットアップと Ghost FTP で使用する言語を選択してください。"],
        ["ko"] = ["환영합니다", "사용권 계약", "사용권 조건에 동의합니다", "뒤로", "다음", "설치 옵션", "설치 준비 완료", "마침", "클라이언트 언어", "설치 프로그램과 Ghost FTP에서 사용할 언어를 선택하세요."],
        ["zh-CN"] = ["欢迎", "许可协议", "我接受许可条款", "上一步", "下一步", "安装选项", "准备安装", "完成", "客户端语言", "选择安装程序和 Ghost FTP 使用的语言。"],
        ["zh-TW"] = ["歡迎", "授權合約", "我接受授權條款", "上一步", "下一步", "安裝選項", "準備安裝", "完成", "用戶端語言", "選擇安裝程式與 Ghost FTP 使用的語言。"]
    };

    public static string T(string key)
    {
        var code = GhostLocalization.CurrentLanguageCode;
        var index = Array.IndexOf(Keys, key);
        if (index < 0)
            return key;

        if (Values.TryGetValue(code, out var localized) && localized.Length == Keys.Length)
            return localized[index];
        return Values[GhostLocalization.DefaultLanguageCode][index];
    }

    public static bool HasCoverage(string languageCode)
    {
        languageCode = GhostLocalization.NormalizeLanguageCode(languageCode);
        return Values.TryGetValue(languageCode, out var localized)
            && localized.Length == Keys.Length
            && localized.All(value => !string.IsNullOrWhiteSpace(value));
    }
}
