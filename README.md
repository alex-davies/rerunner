# rerunner
Reruns .net executable when the file changes.

`rerunner.exe "C:\some\path\to\a\dot\net\executable.exe"`

Will run the exe without locking the files. When either the exe changes or anything in it's the folder is added or changed the exe is rerun.

This is primarily used to assist in development of self hosted websites. `rerunner.exe` is pointed at the output of the self hosted website and the site is restarted after build.
