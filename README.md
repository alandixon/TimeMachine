# SetTime

Simple .Net Core app to read the time from an ntp server and set it locally. <br>Actually, it tries several ntp servers and uses the one that replies fastest.

## Why?

Because the rtc battery on my laptop has failed and it's too fiddly to replace. I've already tried once. The laptop typically boots up in the year 2059, the browser starts up and gets very upset because all the certificates are badly wrong (I often have several tabs left over from my last session). It deletes a load of cookies because they're out of date and then I have to go through the pain of telling some sites I don't want their cookies (again).

## Why doesn't Windows update the time?

It does in the end, but because the time is so far out, it seems unhappy to do it quick enough for me to start work in the correct year.

## Portability

The code is .Net 9.0 apart from the actual time setting which is an admin level interop call on kernel32.dll, so is only compatible with Windows.

## Permissions

The time change requires admin rights.
<br>The easiest way to do this at startup is with Task scheduler; give this task the highest privileges.

## Credits

[This neat approach to async multi ntp servers](https://stackoverflow.com/questions/40627941/asynchronous-operations-in-a-console-application/40630963#40630963)
