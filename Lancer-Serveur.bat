@echo off
title Serveur Presence Etudiant
cd /d "%~dp0Api"
echo.
echo  Demarrage du serveur Presence Etudiant...
echo  (la premiere fois, patiente 1 a 2 minutes : telechargement des paquets)
echo  Garde cette fenetre ouverte. Pour arreter : ferme-la.
echo.
dotnet run
pause
