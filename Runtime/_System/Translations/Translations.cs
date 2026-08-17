#nullable enable

using System.Collections.Generic;

namespace FlappyTemplate
{
    // Every string the package's own windows show, in every locale ELocale names.
    //
    // One Row per key, one named argument per language. The arguments are all required, so a key added without
    // a Georgian string does not compile - which is the rule this file exists to enforce: a translation is
    // added to the list for every locale at once, or it is not added. Named arguments rather than positional
    // ones because a row is 22 strings long and a language shifted by one is a bug nobody would see until a
    // player did.
    //
    // Adding a string:
    //
    //   1. Name a key - area.thing, lower case, e.g. shop.buy_button.
    //   2. Write a Row for it below, in the block for its area, with all 22 languages filled in.
    //   3. Use it: Translator.T("shop.buy_button").
    //
    // A game keeps its own strings out of here and registers them instead - Translator.Add - so that updating
    // the package does not walk over them. See the README beside this file.
    public static class Translations
    {
        private static readonly Dictionary<ELocale, Dictionary<string, string>> byLocale = new();
        private static readonly List<string> keys = new();

        static Translations()
        {
            // Shared - a word that reads the same in the fairness window and the bet info window is one key,
            // so a better wording for it is a one-line change rather than a hunt.

            Row("common.na",
                en_US: "N/A", ru_RU: "Н/Д", fr_FR: "N/D", bn_BD: "প্রযোজ্য নয়",
                de_DE: "K/A", es_ES: "N/D", id_ID: "T/A", pt_PT: "N/D",
                tr_TR: "Yok", vi_VN: "Không có", ar_AE: "غير متاح", hi_IN: "लागू नहीं",
                th_TH: "ไม่มี", ja_JP: "該当なし", ko_KR: "해당 없음", zh_CN: "无",
                fil_PH: "Wala", ur_PK: "دستیاب نہیں", fa_IR: "موجود نیست", ms_MY: "Tiada",
                ka_GE: "არ არის", hy_AM: "Չկա");

            Row("common.hidden",
                en_US: "Hidden", ru_RU: "Скрыт", fr_FR: "Masqué", bn_BD: "গোপন",
                de_DE: "Verborgen", es_ES: "Oculto", id_ID: "Tersembunyi", pt_PT: "Oculto",
                tr_TR: "Gizli", vi_VN: "Đã ẩn", ar_AE: "مخفي", hi_IN: "छिपा हुआ",
                th_TH: "ซ่อนอยู่", ja_JP: "非表示", ko_KR: "숨김", zh_CN: "已隐藏",
                fil_PH: "Nakatago", ur_PK: "پوشیدہ", fa_IR: "پنهان", ms_MY: "Tersembunyi",
                ka_GE: "დამალული", hy_AM: "Թաքցված");

            Row("common.nonce",
                en_US: "Nonce", ru_RU: "Нонс", fr_FR: "Nonce", bn_BD: "নন্স",
                de_DE: "Nonce", es_ES: "Nonce", id_ID: "Nonce", pt_PT: "Nonce",
                tr_TR: "Nonce", vi_VN: "Nonce", ar_AE: "الرقم التسلسلي", hi_IN: "नॉन्स",
                th_TH: "Nonce", ja_JP: "ノンス", ko_KR: "논스", zh_CN: "随机数",
                fil_PH: "Nonce", ur_PK: "نانس", fa_IR: "نانس", ms_MY: "Nonce",
                ka_GE: "ნონსი", hy_AM: "Նոնս");

            Row("common.client_seed",
                en_US: "Client seed", ru_RU: "Клиентский сид", fr_FR: "Graine client", bn_BD: "ক্লায়েন্ট সিড",
                de_DE: "Client-Seed", es_ES: "Semilla del cliente", id_ID: "Seed klien", pt_PT: "Semente do cliente",
                tr_TR: "İstemci tohumu", vi_VN: "Seed khách hàng", ar_AE: "بذرة العميل", hi_IN: "क्लाइंट सीड",
                th_TH: "ซีดของผู้เล่น", ja_JP: "クライアントシード", ko_KR: "클라이언트 시드", zh_CN: "客户端种子",
                fil_PH: "Client seed", ur_PK: "کلائنٹ سیڈ", fa_IR: "بذر کلاینت", ms_MY: "Benih klien",
                ka_GE: "კლიენტის სიდი", hy_AM: "Հաճախորդի սերմ");

            Row("common.server_seed",
                en_US: "Server seed", ru_RU: "Серверный сид", fr_FR: "Graine serveur", bn_BD: "সার্ভার সিড",
                de_DE: "Server-Seed", es_ES: "Semilla del servidor", id_ID: "Seed server", pt_PT: "Semente do servidor",
                tr_TR: "Sunucu tohumu", vi_VN: "Seed máy chủ", ar_AE: "بذرة الخادم", hi_IN: "सर्वर सीड",
                th_TH: "ซีดของเซิร์ฟเวอร์", ja_JP: "サーバーシード", ko_KR: "서버 시드", zh_CN: "服务器种子",
                fil_PH: "Server seed", ur_PK: "سرور سیڈ", fa_IR: "بذر سرور", ms_MY: "Benih pelayan",
                ka_GE: "სერვერის სიდი", hy_AM: "Սերվերի սերմ");

            Row("common.block_hash",
                en_US: "Bitcoin last block hash", ru_RU: "Хеш последнего блока Bitcoin", fr_FR: "Hachage du dernier bloc Bitcoin", bn_BD: "বিটকয়েনের শেষ ব্লক হ্যাশ",
                de_DE: "Hash des letzten Bitcoin-Blocks", es_ES: "Hash del último bloque de Bitcoin", id_ID: "Hash blok Bitcoin terakhir", pt_PT: "Hash do último bloco de Bitcoin",
                tr_TR: "Son Bitcoin blok özeti", vi_VN: "Hash khối Bitcoin cuối cùng", ar_AE: "تجزئة آخر كتلة بيتكوين", hi_IN: "बिटकॉइन के अंतिम ब्लॉक का हैश",
                th_TH: "แฮชบล็อกล่าสุดของบิตคอยน์", ja_JP: "ビットコイン最新ブロックのハッシュ", ko_KR: "비트코인 마지막 블록 해시", zh_CN: "比特币最新区块哈希",
                fil_PH: "Hash ng huling Bitcoin block", ur_PK: "بٹ کوائن کے آخری بلاک کا ہیش", fa_IR: "هش آخرین بلاک بیت‌کوین", ms_MY: "Cincangan blok Bitcoin terakhir",
                ka_GE: "Bitcoin-ის ბოლო ბლოკის ჰეში", hy_AM: "Bitcoin-ի վերջին բլոկի հեշը");

            Row("common.server_sha512",
                en_US: "Server SHA-512", ru_RU: "SHA-512 сервера", fr_FR: "SHA-512 du serveur", bn_BD: "সার্ভার SHA-512",
                de_DE: "Server-SHA-512", es_ES: "SHA-512 del servidor", id_ID: "SHA-512 server", pt_PT: "SHA-512 do servidor",
                tr_TR: "Sunucu SHA-512", vi_VN: "SHA-512 máy chủ", ar_AE: "SHA-512 للخادم", hi_IN: "सर्वर SHA-512",
                th_TH: "SHA-512 ของเซิร์ฟเวอร์", ja_JP: "サーバー SHA-512", ko_KR: "서버 SHA-512", zh_CN: "服务器 SHA-512",
                fil_PH: "SHA-512 ng server", ur_PK: "سرور SHA-512", fa_IR: "SHA-512 سرور", ms_MY: "SHA-512 pelayan",
                ka_GE: "სერვერის SHA-512", hy_AM: "Սերվերի SHA-512");

            // Bet info window.

            Row("bet_info.title",
                en_US: "Bet info", ru_RU: "О ставке", fr_FR: "Détails du pari", bn_BD: "বাজির তথ্য",
                de_DE: "Wettdetails", es_ES: "Detalles de la apuesta", id_ID: "Info taruhan", pt_PT: "Detalhes da aposta",
                tr_TR: "Bahis bilgisi", vi_VN: "Thông tin cược", ar_AE: "معلومات الرهان", hi_IN: "बेट की जानकारी",
                th_TH: "ข้อมูลการเดิมพัน", ja_JP: "ベット情報", ko_KR: "베팅 정보", zh_CN: "投注详情",
                fil_PH: "Impormasyon ng taya", ur_PK: "شرط کی معلومات", fa_IR: "اطلاعات شرط", ms_MY: "Maklumat pertaruhan",
                ka_GE: "ფსონის ინფორმაცია", hy_AM: "Խաղադրույքի տվյալները");

            Row("bet_info.profit",
                en_US: "Profit", ru_RU: "Прибыль", fr_FR: "Gain", bn_BD: "লাভ",
                de_DE: "Gewinn", es_ES: "Beneficio", id_ID: "Keuntungan", pt_PT: "Lucro",
                tr_TR: "Kâr", vi_VN: "Lợi nhuận", ar_AE: "الربح", hi_IN: "मुनाफ़ा",
                th_TH: "กำไร", ja_JP: "利益", ko_KR: "수익", zh_CN: "盈利",
                fil_PH: "Tubo", ur_PK: "منافع", fa_IR: "سود", ms_MY: "Keuntungan",
                ka_GE: "მოგება", hy_AM: "Շահույթ");

            Row("bet_info.bet",
                en_US: "Bet", ru_RU: "Ставка", fr_FR: "Mise", bn_BD: "বাজি",
                de_DE: "Einsatz", es_ES: "Apuesta", id_ID: "Taruhan", pt_PT: "Aposta",
                tr_TR: "Bahis", vi_VN: "Tiền cược", ar_AE: "الرهان", hi_IN: "बेट",
                th_TH: "เดิมพัน", ja_JP: "ベット", ko_KR: "베팅", zh_CN: "投注",
                fil_PH: "Taya", ur_PK: "شرط", fa_IR: "شرط", ms_MY: "Pertaruhan",
                ka_GE: "ფსონი", hy_AM: "Խաղադրույք");

            Row("bet_info.payout",
                en_US: "Payout", ru_RU: "Выплата", fr_FR: "Paiement", bn_BD: "পরিশোধ",
                de_DE: "Auszahlung", es_ES: "Pago", id_ID: "Pembayaran", pt_PT: "Pagamento",
                tr_TR: "Ödeme", vi_VN: "Tiền thắng", ar_AE: "الدفع", hi_IN: "भुगतान",
                th_TH: "เงินรางวัล", ja_JP: "払い戻し", ko_KR: "지급액", zh_CN: "派彩",
                fil_PH: "Bayad", ur_PK: "ادائیگی", fa_IR: "پرداخت", ms_MY: "Pembayaran",
                ka_GE: "გადახდა", hy_AM: "Վճարում");

            Row("bet_info.player",
                en_US: "Player", ru_RU: "Игрок", fr_FR: "Joueur", bn_BD: "খেলোয়াড়",
                de_DE: "Spieler", es_ES: "Jugador", id_ID: "Pemain", pt_PT: "Jogador",
                tr_TR: "Oyuncu", vi_VN: "Người chơi", ar_AE: "اللاعب", hi_IN: "खिलाड़ी",
                th_TH: "ผู้เล่น", ja_JP: "プレイヤー", ko_KR: "플레이어", zh_CN: "玩家",
                fil_PH: "Manlalaro", ur_PK: "کھلاڑی", fa_IR: "بازیکن", ms_MY: "Pemain",
                ka_GE: "მოთამაშე", hy_AM: "Խաղացող");

            Row("bet_info.bet_id",
                en_US: "Bet's id", ru_RU: "ID ставки", fr_FR: "ID du pari", bn_BD: "বাজির আইডি",
                de_DE: "Wett-ID", es_ES: "ID de la apuesta", id_ID: "ID taruhan", pt_PT: "ID da aposta",
                tr_TR: "Bahis kimliği", vi_VN: "ID cược", ar_AE: "معرّف الرهان", hi_IN: "बेट आईडी",
                th_TH: "รหัสการเดิมพัน", ja_JP: "ベットID", ko_KR: "베팅 ID", zh_CN: "投注 ID",
                fil_PH: "ID ng taya", ur_PK: "شرط کی آئی ڈی", fa_IR: "شناسه شرط", ms_MY: "ID pertaruhan",
                ka_GE: "ფსონის ID", hy_AM: "Խաղադրույքի ID");

            Row("bet_info.game",
                en_US: "Game", ru_RU: "Игра", fr_FR: "Jeu", bn_BD: "গেম",
                de_DE: "Spiel", es_ES: "Juego", id_ID: "Permainan", pt_PT: "Jogo",
                tr_TR: "Oyun", vi_VN: "Trò chơi", ar_AE: "اللعبة", hi_IN: "गेम",
                th_TH: "เกม", ja_JP: "ゲーム", ko_KR: "게임", zh_CN: "游戏",
                fil_PH: "Laro", ur_PK: "گیم", fa_IR: "بازی", ms_MY: "Permainan",
                ka_GE: "თამაში", hy_AM: "Խաղ");

            Row("bet_info.time",
                en_US: "Time", ru_RU: "Время", fr_FR: "Heure", bn_BD: "সময়",
                de_DE: "Zeit", es_ES: "Hora", id_ID: "Waktu", pt_PT: "Hora",
                tr_TR: "Saat", vi_VN: "Thời gian", ar_AE: "الوقت", hi_IN: "समय",
                th_TH: "เวลา", ja_JP: "時刻", ko_KR: "시간", zh_CN: "时间",
                fil_PH: "Oras", ur_PK: "وقت", fa_IR: "زمان", ms_MY: "Masa",
                ka_GE: "დრო", hy_AM: "Ժամ");

            Row("bet_info.details",
                en_US: "Details", ru_RU: "Подробности", fr_FR: "Détails", bn_BD: "বিবরণ",
                de_DE: "Details", es_ES: "Detalles", id_ID: "Detail", pt_PT: "Detalhes",
                tr_TR: "Ayrıntılar", vi_VN: "Chi tiết", ar_AE: "التفاصيل", hi_IN: "विवरण",
                th_TH: "รายละเอียด", ja_JP: "詳細", ko_KR: "상세 정보", zh_CN: "详情",
                fil_PH: "Mga detalye", ur_PK: "تفصیلات", fa_IR: "جزئیات", ms_MY: "Butiran",
                ka_GE: "დეტალები", hy_AM: "Մանրամասներ");

            Row("bet_info.verify",
                en_US: "Verify", ru_RU: "Проверить", fr_FR: "Vérifier", bn_BD: "যাচাই করুন",
                de_DE: "Prüfen", es_ES: "Verificar", id_ID: "Verifikasi", pt_PT: "Verificar",
                tr_TR: "Doğrula", vi_VN: "Xác minh", ar_AE: "تحقق", hi_IN: "सत्यापित करें",
                th_TH: "ตรวจสอบ", ja_JP: "検証", ko_KR: "검증", zh_CN: "验证",
                fil_PH: "I-verify", ur_PK: "تصدیق کریں", fa_IR: "بررسی", ms_MY: "Sahkan",
                ka_GE: "შემოწმება", hy_AM: "Ստուգել");

            // Game history window.

            Row("game_history.title",
                en_US: "Game history", ru_RU: "История игры", fr_FR: "Historique du jeu", bn_BD: "গেমের ইতিহাস",
                de_DE: "Spielverlauf", es_ES: "Historial del juego", id_ID: "Riwayat permainan", pt_PT: "Histórico do jogo",
                tr_TR: "Oyun geçmişi", vi_VN: "Lịch sử trò chơi", ar_AE: "سجل اللعبة", hi_IN: "गेम इतिहास",
                th_TH: "ประวัติเกม", ja_JP: "ゲーム履歴", ko_KR: "게임 기록", zh_CN: "游戏历史",
                fil_PH: "Kasaysayan ng laro", ur_PK: "گیم کی تاریخ", fa_IR: "تاریخچه بازی", ms_MY: "Sejarah permainan",
                ka_GE: "თამაშის ისტორია", hy_AM: "Խաղի պատմություն");

            Row("game_history.total_bet_count",
                en_US: "Total bet count", ru_RU: "Всего ставок", fr_FR: "Nombre total de paris", bn_BD: "মোট বাজির সংখ্যা",
                de_DE: "Anzahl der Wetten", es_ES: "Número total de apuestas", id_ID: "Jumlah taruhan", pt_PT: "Número total de apostas",
                tr_TR: "Toplam bahis sayısı", vi_VN: "Tổng số cược", ar_AE: "إجمالي عدد الرهانات", hi_IN: "कुल बेट संख्या",
                th_TH: "จำนวนเดิมพันทั้งหมด", ja_JP: "ベット総数", ko_KR: "총 베팅 수", zh_CN: "总投注笔数",
                fil_PH: "Kabuuang bilang ng taya", ur_PK: "کل شرطوں کی تعداد", fa_IR: "تعداد کل شرط‌ها", ms_MY: "Jumlah bilangan pertaruhan",
                ka_GE: "ფსონების საერთო რაოდენობა", hy_AM: "Խաղադրույքների ընդհանուր թիվը");

            Row("game_history.total_bet_amount",
                en_US: "Total bet amount", ru_RU: "Сумма ставок", fr_FR: "Montant total misé", bn_BD: "মোট বাজির পরিমাণ",
                de_DE: "Gesamteinsatz", es_ES: "Importe total apostado", id_ID: "Total nilai taruhan", pt_PT: "Valor total apostado",
                tr_TR: "Toplam bahis tutarı", vi_VN: "Tổng tiền cược", ar_AE: "إجمالي مبلغ الرهان", hi_IN: "कुल बेट राशि",
                th_TH: "ยอดเดิมพันรวม", ja_JP: "ベット総額", ko_KR: "총 베팅 금액", zh_CN: "总投注金额",
                fil_PH: "Kabuuang halaga ng taya", ur_PK: "کل شرط کی رقم", fa_IR: "مبلغ کل شرط‌ها", ms_MY: "Jumlah amaun pertaruhan",
                ka_GE: "ფსონების საერთო თანხა", hy_AM: "Խաղադրույքների ընդհանուր գումարը");

            Row("game_history.total_profit",
                en_US: "Total profit", ru_RU: "Общая прибыль", fr_FR: "Gain total", bn_BD: "মোট লাভ",
                de_DE: "Gesamtgewinn", es_ES: "Beneficio total", id_ID: "Total keuntungan", pt_PT: "Lucro total",
                tr_TR: "Toplam kâr", vi_VN: "Tổng lợi nhuận", ar_AE: "إجمالي الربح", hi_IN: "कुल मुनाफ़ा",
                th_TH: "กำไรรวม", ja_JP: "合計利益", ko_KR: "총 수익", zh_CN: "总盈利",
                fil_PH: "Kabuuang tubo", ur_PK: "کل منافع", fa_IR: "سود کل", ms_MY: "Jumlah keuntungan",
                ka_GE: "საერთო მოგება", hy_AM: "Ընդհանուր շահույթ");

            Row("game_history.no_bets",
                en_US: "No bets on this round", ru_RU: "На этот раунд ставок нет", fr_FR: "Aucun pari sur ce tour", bn_BD: "এই রাউন্ডে কোনো বাজি নেই",
                de_DE: "Keine Wetten in dieser Runde", es_ES: "Sin apuestas en esta ronda", id_ID: "Tidak ada taruhan di ronde ini", pt_PT: "Sem apostas nesta ronda",
                tr_TR: "Bu turda bahis yok", vi_VN: "Không có cược ở vòng này", ar_AE: "لا رهانات في هذه الجولة", hi_IN: "इस राउंड में कोई बेट नहीं",
                th_TH: "ไม่มีการเดิมพันในรอบนี้", ja_JP: "このラウンドのベットはありません", ko_KR: "이 라운드에 베팅이 없습니다", zh_CN: "本局没有投注",
                fil_PH: "Walang taya sa round na ito", ur_PK: "اس راؤنڈ میں کوئی شرط نہیں", fa_IR: "در این دور شرطی وجود ندارد", ms_MY: "Tiada pertaruhan pada pusingan ini",
                ka_GE: "ამ რაუნდზე ფსონები არ არის", hy_AM: "Այս ռաունդում խաղադրույքներ չկան");

            // Fairness window.

            Row("fairness.title",
                en_US: "Fairness", ru_RU: "Честность", fr_FR: "Équité", bn_BD: "ন্যায্যতা",
                de_DE: "Fairness", es_ES: "Equidad", id_ID: "Keadilan", pt_PT: "Equidade",
                tr_TR: "Adillik", vi_VN: "Công bằng", ar_AE: "النزاهة", hi_IN: "निष्पक्षता",
                th_TH: "ความยุติธรรม", ja_JP: "公正性", ko_KR: "공정성", zh_CN: "公平性",
                fil_PH: "Pagiging patas", ur_PK: "شفافیت", fa_IR: "انصاف", ms_MY: "Keadilan",
                ka_GE: "სამართლიანობა", hy_AM: "Արդարություն");

            Row("fairness.new_client_seed",
                en_US: "New client seed", ru_RU: "Новый клиентский сид", fr_FR: "Nouvelle graine client", bn_BD: "নতুন ক্লায়েন্ট সিড",
                de_DE: "Neuer Client-Seed", es_ES: "Nueva semilla del cliente", id_ID: "Seed klien baru", pt_PT: "Nova semente do cliente",
                tr_TR: "Yeni istemci tohumu", vi_VN: "Seed khách hàng mới", ar_AE: "بذرة عميل جديدة", hi_IN: "नया क्लाइंट सीड",
                th_TH: "ซีดของผู้เล่นใหม่", ja_JP: "新しいクライアントシード", ko_KR: "새 클라이언트 시드", zh_CN: "新客户端种子",
                fil_PH: "Bagong client seed", ur_PK: "نیا کلائنٹ سیڈ", fa_IR: "بذر کلاینت جدید", ms_MY: "Benih klien baharu",
                ka_GE: "ახალი კლიენტის სიდი", hy_AM: "Նոր հաճախորդի սերմ");

            Row("fairness.randomize",
                en_US: "Randomize", ru_RU: "Случайно", fr_FR: "Aléatoire", bn_BD: "এলোমেলো করুন",
                de_DE: "Zufällig", es_ES: "Aleatorizar", id_ID: "Acak", pt_PT: "Aleatorizar",
                tr_TR: "Rastgele", vi_VN: "Ngẫu nhiên", ar_AE: "عشوائي", hi_IN: "यादृच्छिक करें",
                th_TH: "สุ่ม", ja_JP: "ランダム", ko_KR: "무작위", zh_CN: "随机生成",
                fil_PH: "I-random", ur_PK: "بے ترتیب", fa_IR: "تصادفی", ms_MY: "Rawak",
                ka_GE: "შემთხვევითი", hy_AM: "Պատահական");

            Row("fairness.current_pair",
                en_US: "Current seed pair", ru_RU: "Текущая пара сидов", fr_FR: "Paire de graines actuelle", bn_BD: "বর্তমান সিড জোড়া",
                de_DE: "Aktuelles Seed-Paar", es_ES: "Par de semillas actual", id_ID: "Pasangan seed saat ini", pt_PT: "Par de sementes atual",
                tr_TR: "Geçerli tohum çifti", vi_VN: "Cặp seed hiện tại", ar_AE: "زوج البذور الحالي", hi_IN: "मौजूदा सीड जोड़ी",
                th_TH: "คู่ซีดปัจจุบัน", ja_JP: "現在のシードペア", ko_KR: "현재 시드 쌍", zh_CN: "当前种子对",
                fil_PH: "Kasalukuyang seed pair", ur_PK: "موجودہ سیڈ جوڑا", fa_IR: "جفت بذر فعلی", ms_MY: "Pasangan benih semasa",
                ka_GE: "მიმდინარე სიდების წყვილი", hy_AM: "Ընթացիկ սերմերի զույգ");

            Row("fairness.previous_pair",
                en_US: "Previous seed pair", ru_RU: "Предыдущая пара сидов", fr_FR: "Paire de graines précédente", bn_BD: "পূর্ববর্তী সিড জোড়া",
                de_DE: "Vorheriges Seed-Paar", es_ES: "Par de semillas anterior", id_ID: "Pasangan seed sebelumnya", pt_PT: "Par de sementes anterior",
                tr_TR: "Önceki tohum çifti", vi_VN: "Cặp seed trước", ar_AE: "زوج البذور السابق", hi_IN: "पिछली सीड जोड़ी",
                th_TH: "คู่ซีดก่อนหน้า", ja_JP: "前回のシードペア", ko_KR: "이전 시드 쌍", zh_CN: "上一个种子对",
                fil_PH: "Nakaraang seed pair", ur_PK: "پچھلا سیڈ جوڑا", fa_IR: "جفت بذر قبلی", ms_MY: "Pasangan benih sebelumnya",
                ka_GE: "წინა სიდების წყვილი", hy_AM: "Նախորդ սերմերի զույգ");

            Row("fairness.server_sha512",
                en_US: "Server seed's SHA512 hash", ru_RU: "SHA512-хеш серверного сида", fr_FR: "Hachage SHA512 de la graine serveur", bn_BD: "সার্ভার সিডের SHA512 হ্যাশ",
                de_DE: "SHA512-Hash des Server-Seeds", es_ES: "Hash SHA512 de la semilla del servidor", id_ID: "Hash SHA512 seed server", pt_PT: "Hash SHA512 da semente do servidor",
                tr_TR: "Sunucu tohumunun SHA512 özeti", vi_VN: "Hash SHA512 của seed máy chủ", ar_AE: "تجزئة SHA512 لبذرة الخادم", hi_IN: "सर्वर सीड का SHA512 हैश",
                th_TH: "แฮช SHA512 ของซีดเซิร์ฟเวอร์", ja_JP: "サーバーシードの SHA512 ハッシュ", ko_KR: "서버 시드의 SHA512 해시", zh_CN: "服务器种子的 SHA512 哈希",
                fil_PH: "SHA512 hash ng server seed", ur_PK: "سرور سیڈ کا SHA512 ہیش", fa_IR: "هش SHA512 بذر سرور", ms_MY: "Cincangan SHA512 benih pelayan",
                ka_GE: "სერვერის სიდის SHA512 ჰეში", hy_AM: "Սերվերի սերմի SHA512 հեշը");

            Row("fairness.bets_made",
                en_US: "Bets made with pair", ru_RU: "Ставок с этой парой", fr_FR: "Paris faits avec cette paire", bn_BD: "এই জোড়া দিয়ে করা বাজি",
                de_DE: "Wetten mit diesem Paar", es_ES: "Apuestas hechas con el par", id_ID: "Taruhan dengan pasangan ini", pt_PT: "Apostas feitas com o par",
                tr_TR: "Bu çiftle yapılan bahisler", vi_VN: "Số cược với cặp này", ar_AE: "الرهانات بهذا الزوج", hi_IN: "इस जोड़ी से लगाई गई बेट",
                th_TH: "การเดิมพันด้วยคู่ซีดนี้", ja_JP: "このペアでのベット数", ko_KR: "이 시드 쌍으로 한 베팅", zh_CN: "使用该种子对的投注数",
                fil_PH: "Mga tayang ginawa sa pares na ito", ur_PK: "اس جوڑے سے لگائی گئی شرطیں", fa_IR: "شرط‌های انجام‌شده با این جفت", ms_MY: "Pertaruhan dengan pasangan ini",
                ka_GE: "ამ წყვილით გაკეთებული ფსონები", hy_AM: "Այս զույգով կատարված խաղադրույքներ");

            // Statistics window.

            Row("statistics.title",
                en_US: "Statistics", ru_RU: "Статистика", fr_FR: "Statistiques", bn_BD: "পরিসংখ্যান",
                de_DE: "Statistik", es_ES: "Estadísticas", id_ID: "Statistik", pt_PT: "Estatísticas",
                tr_TR: "İstatistikler", vi_VN: "Thống kê", ar_AE: "الإحصائيات", hi_IN: "आँकड़े",
                th_TH: "สถิติ", ja_JP: "統計", ko_KR: "통계", zh_CN: "统计",
                fil_PH: "Estadistika", ur_PK: "اعداد و شمار", fa_IR: "آمار", ms_MY: "Statistik",
                ka_GE: "სტატისტიკა", hy_AM: "Վիճակագրություն");

            Row("statistics.current",
                en_US: "Current", ru_RU: "Текущая", fr_FR: "Actuel", bn_BD: "বর্তমান",
                de_DE: "Aktuell", es_ES: "Actual", id_ID: "Saat ini", pt_PT: "Atual",
                tr_TR: "Güncel", vi_VN: "Hiện tại", ar_AE: "الحالي", hi_IN: "मौजूदा",
                th_TH: "ปัจจุบัน", ja_JP: "現在", ko_KR: "현재", zh_CN: "当前",
                fil_PH: "Kasalukuyan", ur_PK: "موجودہ", fa_IR: "فعلی", ms_MY: "Semasa",
                ka_GE: "მიმდინარე", hy_AM: "Ընթացիկ");

            Row("statistics.overall",
                en_US: "Overall", ru_RU: "Общая", fr_FR: "Global", bn_BD: "সামগ্রিক",
                de_DE: "Gesamt", es_ES: "Total", id_ID: "Keseluruhan", pt_PT: "Geral",
                tr_TR: "Genel", vi_VN: "Tổng thể", ar_AE: "الإجمالي", hi_IN: "कुल",
                th_TH: "ทั้งหมด", ja_JP: "全体", ko_KR: "전체", zh_CN: "总计",
                fil_PH: "Kabuuan", ur_PK: "مجموعی", fa_IR: "کلی", ms_MY: "Keseluruhan",
                ka_GE: "საერთო", hy_AM: "Ընդհանուր");

            Row("statistics.total_wagered",
                en_US: "Total wagered", ru_RU: "Всего поставлено", fr_FR: "Total misé", bn_BD: "মোট বাজি ধরা",
                de_DE: "Gesamteinsatz", es_ES: "Total apostado", id_ID: "Total taruhan", pt_PT: "Total apostado",
                tr_TR: "Toplam bahis", vi_VN: "Tổng tiền cược", ar_AE: "إجمالي الرهانات", hi_IN: "कुल दांव",
                th_TH: "ยอดเดิมพันรวม", ja_JP: "合計ベット額", ko_KR: "총 베팅액", zh_CN: "总投注额",
                fil_PH: "Kabuuang taya", ur_PK: "کل شرط", fa_IR: "مجموع شرط‌بندی", ms_MY: "Jumlah pertaruhan",
                ka_GE: "სულ დადებული", hy_AM: "Ընդհանուր խաղադրույք");

            Row("statistics.counts",
                en_US: "Bets / Wins / Losses", ru_RU: "Ставки / Победы / Поражения", fr_FR: "Paris / Gains / Pertes", bn_BD: "বাজি / জয় / হার",
                de_DE: "Wetten / Gewinne / Verluste", es_ES: "Apuestas / Victorias / Derrotas", id_ID: "Taruhan / Menang / Kalah", pt_PT: "Apostas / Vitórias / Derrotas",
                tr_TR: "Bahis / Kazanç / Kayıp", vi_VN: "Cược / Thắng / Thua", ar_AE: "الرهانات / الفوز / الخسارة", hi_IN: "बेट / जीत / हार",
                th_TH: "เดิมพัน / ชนะ / แพ้", ja_JP: "ベット / 勝ち / 負け", ko_KR: "베팅 / 승 / 패", zh_CN: "投注 / 胜 / 负",
                fil_PH: "Taya / Panalo / Talo", ur_PK: "شرطیں / جیت / ہار", fa_IR: "شرط‌ها / بردها / باخت‌ها", ms_MY: "Pertaruhan / Menang / Kalah",
                ka_GE: "ფსონები / მოგება / წაგება", hy_AM: "Խաղադրույքներ / Հաղթանակ / Պարտություն");

            Row("statistics.revenue",
                en_US: "Revenue", ru_RU: "Доход", fr_FR: "Revenus", bn_BD: "আয়",
                de_DE: "Ertrag", es_ES: "Ingresos", id_ID: "Pendapatan", pt_PT: "Receita",
                tr_TR: "Gelir", vi_VN: "Doanh thu", ar_AE: "الإيرادات", hi_IN: "आय",
                th_TH: "รายรับ", ja_JP: "収益", ko_KR: "수입", zh_CN: "收益",
                fil_PH: "Kita", ur_PK: "آمدنی", fa_IR: "درآمد", ms_MY: "Pendapatan",
                ka_GE: "შემოსავალი", hy_AM: "Եկամուտ");

            Row("statistics.total_profit",
                en_US: "Total profit", ru_RU: "Общая прибыль", fr_FR: "Bénéfice total", bn_BD: "মোট লাভ",
                de_DE: "Gesamtgewinn", es_ES: "Beneficio total", id_ID: "Total keuntungan", pt_PT: "Lucro total",
                tr_TR: "Toplam kâr", vi_VN: "Tổng lợi nhuận", ar_AE: "إجمالي الربح", hi_IN: "कुल मुनाफ़ा",
                th_TH: "กำไรรวม", ja_JP: "合計利益", ko_KR: "총 수익", zh_CN: "总盈利",
                fil_PH: "Kabuuang tubo", ur_PK: "کل منافع", fa_IR: "سود کل", ms_MY: "Jumlah keuntungan",
                ka_GE: "სულ მოგება", hy_AM: "Ընդհանուր շահույթ");

            Row("statistics.luck",
                en_US: "Luck", ru_RU: "Удача", fr_FR: "Chance", bn_BD: "ভাগ্য",
                de_DE: "Glück", es_ES: "Suerte", id_ID: "Keberuntungan", pt_PT: "Sorte",
                tr_TR: "Şans", vi_VN: "May mắn", ar_AE: "الحظ", hi_IN: "भाग्य",
                th_TH: "โชค", ja_JP: "運", ko_KR: "행운", zh_CN: "幸运值",
                fil_PH: "Suwerte", ur_PK: "قسمت", fa_IR: "شانس", ms_MY: "Tuah",
                ka_GE: "იღბალი", hy_AM: "Հաջողություն");

            // Navbar. Its other two captions are statistics.title and fairness.title above - the bar opens
            // those windows, and a button named one thing while the dialog it opens is named another reads
            // as two features.

            Row("navbar.home",
                en_US: "Home", ru_RU: "На главную", fr_FR: "Accueil", bn_BD: "হোম",
                de_DE: "Startseite", es_ES: "Inicio", id_ID: "Beranda", pt_PT: "Início",
                tr_TR: "Ana sayfa", vi_VN: "Trang chủ", ar_AE: "الرئيسية", hi_IN: "होम",
                th_TH: "หน้าหลัก", ja_JP: "ホーム", ko_KR: "홈", zh_CN: "主页",
                fil_PH: "Home", ur_PK: "ہوم", fa_IR: "خانه", ms_MY: "Laman utama",
                ka_GE: "მთავარი", hy_AM: "Գլխավոր");
        }

        /// <summary>Every key the package ships, in the order they are written above.</summary>
        public static IReadOnlyList<string> Keys => keys;

        /// <summary>The package's text for a key in one locale, with no fallback of its own -
        /// <see cref="Translator"/> does the falling back.</summary>
        public static bool Find(ELocale locale, string key, out string text)
        {
            if (byLocale.TryGetValue(locale, out var table) && table.TryGetValue(key, out var found))
            {
                text = found ?? string.Empty;
                return true;
            }

            text = string.Empty;
            return false;
        }

        // One key in every language there is. Every parameter is required, which is what keeps this file whole.
        private static void Row(
            string key,
            string en_US, string ru_RU, string fr_FR, string bn_BD,
            string de_DE, string es_ES, string id_ID, string pt_PT,
            string tr_TR, string vi_VN, string ar_AE, string hi_IN,
            string th_TH, string ja_JP, string ko_KR, string zh_CN,
            string fil_PH, string ur_PK, string fa_IR, string ms_MY,
            string ka_GE, string hy_AM)
        {
            keys.Add(key);

            Put(ELocale.en_US, key, en_US);
            Put(ELocale.ru_RU, key, ru_RU);
            Put(ELocale.fr_FR, key, fr_FR);
            Put(ELocale.bn_BD, key, bn_BD);
            Put(ELocale.de_DE, key, de_DE);
            Put(ELocale.es_ES, key, es_ES);
            Put(ELocale.id_ID, key, id_ID);
            Put(ELocale.pt_PT, key, pt_PT);
            Put(ELocale.tr_TR, key, tr_TR);
            Put(ELocale.vi_VN, key, vi_VN);
            Put(ELocale.ar_AE, key, ar_AE);
            Put(ELocale.hi_IN, key, hi_IN);
            Put(ELocale.th_TH, key, th_TH);
            Put(ELocale.ja_JP, key, ja_JP);
            Put(ELocale.ko_KR, key, ko_KR);
            Put(ELocale.zh_CN, key, zh_CN);
            Put(ELocale.fil_PH, key, fil_PH);
            Put(ELocale.ur_PK, key, ur_PK);
            Put(ELocale.fa_IR, key, fa_IR);
            Put(ELocale.ms_MY, key, ms_MY);
            Put(ELocale.ka_GE, key, ka_GE);
            Put(ELocale.hy_AM, key, hy_AM);
        }

        private static void Put(ELocale locale, string key, string text)
        {
            if (!byLocale.TryGetValue(locale, out var table))
                byLocale[locale] = table = new Dictionary<string, string>();

            table[key] = text;
        }
    }
}
