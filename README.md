# SQL-Server-Job-Monitor

O intuito deste projeto era contornar a situação em que os Jobs do BI terminavam com falha na madrugata, atrasando outros Jobs e deixando o sistema do BI lento.

A aplicação começa com um loop de duas tentativas com 5 minutos de duração cada.
Dentro do loop ele chama o método ConnectDB() que realiza a conexão com o nosso banco de dados para realizar a consulta em uma tabela de status dos jobs.

Após, faz a checagem se há algum job com erro (STATUS 0)  e grava em uma lista auxiliar, pois alguns jobs possuiem um retry.

Se na segunda checagem o job ainda estiver com falha e estiver na lista auxiliar, ele chama o método MakeCall(dest) com o nome do atual plantonista.

No método MakeCall() ele faz a conexão com o WebService da Cisco WebDialer pelo link https://IP/webdialer/services/WebdialerSoapService?wsdl. Utilizando as credenciasis do Cisco Call Manager.

No Call Manager há um desvio de chamada do ramal do plantonista para o seu celular.

Sendo assim, quando um jobs estiver com erro o celular do plantonista irá tocar.
