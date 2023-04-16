# SQL-Server-Job-Monitor

## O Problema
Jobs do SQL Server finalizavam com erro na madrugada, atrasando a sua entrega e a de outros, e consequentemente colocando o sistema do BI em congestionamento no início do dia.

## A solução
Já utilizávamos um sistema de monitoramento por terceiro, mas por problemas estávamos sem este suporte.

Com uma tabela de controle, que era utilizada pelo sistema de monitoramento anterior, eu tinha todas as informações que precisava. Nome do job, status, nome do plantonista e telefone. Bastava um script ler essas informações, e quando um job relatar erro, realizar uma ligação para o plantonista.


### Cisco WebDialer
WebDialer é um serviço da cisco que faz parte da instalação geral do servidor Unified Communications Manager (Unified CM). Ele permite que os usuários façam chamadas de de *click to disk* (C2D) em uma página de diretório corporativo ou como parte de um aplicativo de desktop personalizado.

### O Script
Atravéz da API do WebDialer eu consigo acessar os métodos como *makeCallSoap, endCallSoap, getProfileSoap*, etc.

Inicio a lógica com um loop para que seja realizada 2 tentativas, a primeira para verificar se um job está com falha e a segunda para confirmar a falha e realizar a ligação.
> Alguns jobs possuem *retry* caso haja problemas de conexão e outros erros irrelevantes.

Dentro do loop eu chamo o método `ConnectDB()`, que faz a conexão com o nosso banco de dados e a leitura da tabela de controle que mencionei anteriormente.
Armazeno o resultado do **SqlDataReader** em uma variável e a passo pelo método *CheckJobs(reader)*.

O método *CheckJobs(reader)* verifica se a contagem de elementos da lista auxiliar **jobList** é maior que zero, se verdadeiro, realiza a ligação para o plantonista atravéz do método *MakeCall(dest)*, se falso, consulta o **SqlDataReader** e verifica se algum Job possui o status de falha. Se verdadeiro, armazena-o na lista **jobList** para ser consultado posteriormente no segundo loop, se falso encerra o método.

O método *MakeCall(dest)* recebe o nome do plantonista atual e define a variável *number* com o número do ramal do plantonista atual. Inicia uma instância do **WebdialerSoapServiceClient** e define as credenciais a serem utilizadas pelo WebService. Por fim, passo as credenciais e o número pelo método da API *makeCallSoap* que realiza a ligação para o telefone ramal do plantonista.
> Nas configurações do Call Manager, a linha do plantonista está com desvio para o seu celular.

*O método AllwaysGoodCertificate garante que o certificado exigido durante a conexão com o CUCM seja sempre de confiança.*
