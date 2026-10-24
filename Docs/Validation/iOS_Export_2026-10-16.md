# Export iOS — 16 octobre 2026

## Résultat

L’export Unity `BuildTarget.iOS` a été lancé avec Unity 2022.3.62f3, `BuildOptions.None`, vers `/tmp/PolyRiskIOSExport_20261016`. Unity a généré une structure Xcode (`Unity-iPhone.xcodeproj`, `Info.plist`, sources IL2CPP), mais le build a échoué pendant la compilation Burst AOT (`ExitCode 6`). Aucun marqueur de build réussi n’a été émis.

Cause bloquante dans `/tmp/PolyRiskIOSExport_20261016.log` :

```text
Failed to determine iphoneos sdk installation path
xcrun: error: SDK "iphoneos" cannot be located
```

Unity cherche `/Applications/XCode.app/Contents/Developer/.../iPhoneOS.sdk`, qui n’existe pas ici. L’arborescence exportée est partielle et non compilable/installable; ce n’est ni une archive Xcode ni un `.ipa`. Le log relève aussi une indisponibilité de licence Unity CLI, mais l’échec de compilation explicitement remonté est l’absence du SDK iOS.

## Prochaine étape

Installer Xcode complet et le SDK iOS, configurer `xcode-select` sur son dossier Developer, puis relancer l’export depuis Unity. Ouvrir ensuite le projet généré dans Xcode, compiler, signer et installer sur un iPhone pour la recette Safari/appareil réelle. Ne pas publier cette sortie partielle.

