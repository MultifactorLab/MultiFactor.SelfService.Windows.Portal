
# Multifactor self-service windows portal

_Also available in other languages: [English](README.md)_

## Что такое Multifactor self-service windows portal?
MultiFactor SelfService Portal (Windows версия) &mdash; веб-сайт, портал самообслуживания, разработанный и поддерживаемый компанией Мультифактор для самостоятельной регистрации второго фактора аутентификации пользователями внутри корпоративной сети Active Directory.

Портал самообслуживания является частью гибридного 2FA решения сервиса <a href="https://multifactor.ru/" target="_blank">MultiFactor</a>.

* <a href="https://github.com/MultifactorLab/MultiFactor.SelfService.Windows.Portal" target="_blank">Исходный код</a>
* <a href="https://github.com/MultifactorLab/MultiFactor.SelfService.Windows.Portal/releases" target="_blank">Релизы</a>


## Содержание
- [Что такое Multifactor self-service windows portal?](#что-такое-multifactor-self-service-windows-portal?)
- [Функции портала](#функции-портала)
- [Сценарии использования](#сценарии-использования)
- [Установка и конфигурация](#установка-и-конфигурация)
- [Лицензия](#лицензия)

## Функции портала

- Проверка логина и пароля пользователя в домене Active Directory, в том числе в нескольких доменах, если между ними настроены доверительные отношения;
- Настройка второго фактора аутентификации;
- Смена пароля пользователя после подтверждения второго фактора;
- Единая точка входа (Single Sign-On) для корпоративных приложений.

Портал предназначен для установки и работы внутри корпоративной сети.

## Сценарии использования

Портал используется для самостоятельной регистрации второго фактора аутентификации пользователями внутри корпоративной сети, а также выполняет роль единой точки входа для приложений, работающих по технологии Single Sign-On.

После настройки второго фактора, пользователи могут использовать его для безопасного подключения удаленного доступа через VPN, VDI или Remote Desktop.

Смотрите также:
- Двухфакторная аутентификация [Windows VPN со службой Routing and Remote Access Service (RRAS)](https://multifactor.ru/docs/windows-2fa-rras-vpn/)
- Двухфакторная аутентификация [Microsoft Remote Desktop Gateway](https://multifactor.ru/docs/windows-2fa-remote-desktop-gateway/)

## Установка и конфигурация
Информацию о настройке, запуске и дополнительные рекомендации смотрите в [документации](https://multifactor.ru/docs/self-service-portal/windows).

## Лицензия

Портал распространяется бесплатно по лицензии [MIT](LICENSE.md).