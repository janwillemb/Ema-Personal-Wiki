cp platforms/android/app/build/outputs/apk/release/app-release-unsigned.apk /mnt/d/TEMP/unsigned.apk
rm /mnt/d/TEMP/signed.apk
rm /mnt/d/TEMP/ema-personal-wiki.apk
/mnt/c/Java/jdk1.8.0_211/bin/jarsigner -verbose -sigalg SHA1withRSA -digestalg SHA1 -keystore d:/TEMP/com.janwillemboer.ema.keystore d:/TEMP/unsigned.apk ema-free
mv /mnt/d/TEMP/unsigned.apk /mnt/d/TEMP/signed.apk
/mnt/c/Android/build-tools/28.0.3/zipalign.exe -v 4 d:/TEMP/signed.apk d:/TEMP/ema-personal-wiki.apk
