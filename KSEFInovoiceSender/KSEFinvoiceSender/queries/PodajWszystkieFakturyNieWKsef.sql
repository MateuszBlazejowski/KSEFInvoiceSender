-- !!! Do not change parameters and variable names as it will result in the program failure !!!  
            SELECT
                h.Id, h.RodzajF, h.NumerF, h.NabywcaId, h.Nazwa1, h.Nazwa2,
                h.Adres, h.Miasto, h.NIP, h.SprzedawcaId, h.DataWystaw, h.Okres,
                h.Towar, h.Rabat, h.Netto, h.VAT, h.Brutto, h.Zaplacono,
                h.NrKoryg, h.IdRozlicz, h.Status, h.Platnosc, h.TerminPlat,
                h.Opis, h.IstniejePDF, h.Przylacze, h.KSEF,
                p.Id, p.IdFaktury, p.NumerF, p.RokFaktury, p.LpPozycji,
                p.Towar, p.Okres, p.Netto, p.StVAT, p.VAT, p.Brutto,
                p.GTU, p.Ilosc, p.Usluga
            FROM FS@Year h
            LEFT JOIN FS_pozycje@Year p ON p.IdFaktury = h.Id
            WHERE h.Status = 0 AND h.KSEF IS NULL
            ORDER BY h.Id, p.LpPozycji