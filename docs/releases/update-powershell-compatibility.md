### Update-Helfer funktioniert auch beim Start aus PowerShell 7

Der automatische Update-Helfer verwendet jetzt die passenden Windows-PowerShell-Module, unabhängig davon, aus welcher Shell die Anwendung gestartet wurde. Dadurch scheitert die Prüfung vor dem EXE-Austausch nicht mehr an einem fehlenden `Get-FileHash`-Befehl. Die gleiche Korrektur behebt die fehlgeschlagenen Updateprüfungen auf GitHub Actions.
