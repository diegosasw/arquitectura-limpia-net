# arquitectura-limpia

Ver curso en Open Webinars

## SMTP
Para envío de emails de prueba se puede utilizar un servidor SMTP de prueba con Docker
```
docker run --rm -it -p 3001:80 -p 25:25 rnwood/smtp4dev
```

y conectarse a `smtp` en el puerto `25`.

La UI está accesible en http://localhost:3001 para ver los emails enviados.

