![CodeQL](https://github.com/MultifactorLab/MultiFactor.SelfService.Windows.Portal/workflows/CodeQL/badge.svg)

# MultiFactor.SelfService.Windows.Portal

_Also available in other languages: [English](README.md)_

MultiFactor SelfService Portal &mdash; веб-сайт, портал самообслуживания, разработанный и поддерживаемый компанией Мультифактор для самостоятельной регистрации второго фактора аутентификации пользователями внутри корпоративной сети Active Directory.

Портал самообслуживания является частью гибридного 2FA решения сервиса <a href="https://multifactor.ru/" target="_blank">MultiFactor</a>.

* <a href="https://github.com/MultifactorLab/MultiFactor.SelfService.Windows.Portal" target="_blank">Код</a>
* <a href="https://github.com/MultifactorLab/MultiFactor.SelfService.Windows.Portal/releases" target="_blank">Сборка</a>

## Содержание

- [Функции портала](#функции-портала)
- [Первые шаги](#первые-шаги)
- [Требования для установки портала](#требования-для-установки-портала)
- [Конфигурация портала](#конфигурация-портала)
- [Установка портала](#установка-портала)
- [Журналы](#журналы)
- [Доступ к порталу](#доступ-к-порталу)
- [Сценарии использования](#сценарии-использования)
- [Лицензия](#лицензия)

## Функции портала

- Проверка логина и пароля пользователя в домене Active Directory, в том числе в нескольких доменах, если между ними настроены доверительные отношения;
- Настройка второго фактора аутентификации;
- Смена пароля пользователя после подтверждения второго фактора;
- Единая точка входа (Single Sign-On) для корпоративных приложений.

Портал предназначен для установки и работы внутри корпоративной сети.

## Первые шаги

1. Зайдите в <a href="https://multifactor.ru/login" target="_blank">личный кабинет</a> Мультифактора, в разделе **Ресурсы** создайте новый веб-сайт:
  - название и адрес: ``произвольные``;
  - формат токена: ``JwtRS256``.
2. После создания вам будут доступны параметры **ApiKey** и **ApiSecret**, они потребуются для настройки портала.

## Требования для установки портала

- Портал устанавливается на любой Windows сервер начиная с версии 2012 R2;
- Серверу с установленным порталом необходим доступ к хосту ```api.multifactor.ru``` по TCP порту 443 (TLS).
- На сервере должна быть установлена роль Web Server (IIS) с компонентом Application Development -> ASP.NET 4.6;

<img src="https://multifactor.ru/img/self-service-portal/windows-server-asp-net-role.png" width="600"/>

## Конфигурация портала

Параметры работы портала хранятся в файле ```web.config``` в формате XML.

```xml
<portalSettings>
  <!--Название вашей организации-->
  <add key="company-name" value="ACME" />
  <!--Название домена Active Directory для проверки логина и пароля пользователей -->
  <add key="company-domain" value="domain.local" />
  <!--URL адрес логотипа организации -->
  <add key="company-logo-url" value="/mfa/content/images/logo.svg" />

  <!--Запрашивать второй фактор только у пользователей из указанной группы для Single Sign On (второй фактор требуется всем, если удалить настройку)-->
  <!--add key="active-directory-2fa-group" value="2FA Users"/-->

  <!--Использовать номер телефона из Active Directory для отправки одноразового кода в СМС (не используется, если удалить настройку)-->
  <!--add key="use-active-directory-user-phone" value="true"/-->
  <!--add key="use-active-directory-mobile-user-phone" value="true"/-->

  <!--Адрес API Мультифактора -->
  <add key="multifactor-api-url" value="https://api.multifactor.ru" />
  <!-- Параметр API KEY из личного кабинета Мультифактора -->
  <add key="multifactor-api-key" value="" />
  <!-- Параметр API Secret из личного кабинета Мультифактора -->
  <add key="multifactor-api-secret" value="" />

  <!--Доступ к API Мультифактора через HTTP прокси (опционально)-->
  <!--add key="multifactor-api-proxy" value="http://proxy:3128"/-->

  <!-- Уровень логирования: 'Debug', 'Info', 'Warn', 'Error' -->
  <add key="logging-level" value="Info" />
</portalSettings>
```

При включении параметра ```use-active-directory-user-phone``` компонент будет использовать телефон, записанный на вкладке **General**. Формат телефона может быть любым.

<img src="https://multifactor.ru/img/radius-adapter/ra-ad-phone-source.png" width="400px">

При включении параметра ```use-active-directory-mobile-user-phone``` компонент будет использовать телефон, записанный на вкладке **Telephones** в поле **Mobile**. Формат телефона также может быть любым.

<img src="https://multifactor.ru/img/radius-adapter/ra-ad-mobile-phone-source.png" width="400">

## Установка портала

1. Запустите ```Server Manager``` -> ```Tools``` -> ```Internet Information Services (IIS) Manager```;

2. Нажмите правой кнопкой на **Default Web Site** и выберите **Add Application**;
<img src="https://multifactor.ru/img/self-service-portal/windows-iis-add-app.png" width="400"/>

3. Создайте новое приложение:
  * Alias: ```mfa```
  * Physical path: ```путь к папке с порталом```

4. Сохраните и закройте

## Журналы

Журналы работы портала находятся в папке ```Logs```. Если их нет, удостоверьтесь, что папка доступна для записи локальному пользователю ```IIS AppPool\DefaultAppPool```.

## Доступ к порталу

Портал доступен по адресу ```https://ваш_домен.ru/mfa```

## Сценарии использования

Портал используется для самостоятельной регистрации второго фактора аутентификации пользователями внутри корпоративной сети, а также выполняет роль единой точки входа для приложений, работающих по технологии Single Sign-On.

После настройки второго фактора, пользователи могут использовать его для безопасного подключения удаленного доступа через VPN, VDI или Remote Desktop.

Смотрите также:
- Двухфакторная аутентификация [Windows VPN со службой Routing and Remote Access Service (RRAS)](https://multifactor.ru/docs/windows-2fa-rras-vpn/)
- Двухфакторная аутентификация [Microsoft Remote Desktop Gateway](https://multifactor.ru/docs/windows-2fa-remote-desktop-gateway/)

## Лицензия

Портал распространяется бесплатно по лицензии [MIT](LICENSE.md).
