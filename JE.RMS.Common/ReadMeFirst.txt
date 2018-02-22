Hello, Here I am going to explain step by step to setup project and install neccessary components to run the project successfully.

-------------------------------------------------------------------------------------------------------------------------------

AZURE - FUNCTIONS

As we know, we are developing Azure function in web applicaations which create dll file which is comparetively faster than script file.

To setup Azure function using Web Application, go step by step.

1. Install Azure tools for your visual studio, you can do it in 2 ways
	I. using web platform installer,
		a. click the link, https://azure.microsoft.com/en-us/tools/. you will have different options to select visual studio, according to our needs, we need to install 
		MS Azure SDK for .net of version 2.9.6 which is perfect for visual studio 2015, select visual studio 2015, web platform installer automatically start to download
		when you click on visual studio 2015 in the given above link.

		b. run the web platform installer,go to as per instruction to install Azure SDK.

	II. using direct installer packages,
		a. click the link, https://www.microsoft.com/en-us/download/details.aspx?id=54289, download and install neccessary components as per instruction has been given in 
		link. you may be have to go one by one to install.

2. After completing the step 1 - install Azure SDK, you need to install node modules for Azure.
	I. to install node modules using npm, its neccessary to install NPM in your machine, to install nodejs/npm in your machine, you can go step by step using this link
		http://blog.teamtreehouse.com/install-node-js-npm-windows
		you can skip this step, if you have already installed nodejs/npm in your machine.

	II. once npm is installed, open the command prompt,  and write the following command to install mode modules for Azure.
		>npm install -g azure-functions-core-tools

		which installs, azure-functions-core-tools globally in the machine.

3. After completing the step 2, open the project(solution) in visual studio. you willhave 3 projects, 1 is web app, 1 is common, 1 is Azure function "SubmitRewardsRequest".
	Right click on "SubmitRewardsRequest" project. go to Proerties --> Web, 
	set value "C:\Users\<<username>>\AppData\Roaming\npm\node_modules\azure-functions-core-tools\bin\func.exe" to the "start external program".
	set "host start" in command line arguments(if not), 		
	set path for "working directory" here it is path for SubmitRewardsRequest, for e.g. "D:\Just Energy\RMS\JE.RMS\SubmitRewardsRequest".

4. Try to build it, if build successfully, then no need to require to install Nuget Packages, else, you need to install some of Nuget Packages.
	Nuget package: Microsoft.Azure.Webjobs and Microsoft.Azure.Webjobs.Extensions 
	Nuget package: System.Web.Extensions
	Nuget package:System.Net.Http.Formatting.Extension
	
5. That's it, now try to run and debug the "SubmitRewardsRequest" function on your local machine.
	if "Http Function SubmitRewardsRequest: http://<<yourmachineurl>>/api/SubmitRewardsRequest" comes into your console, 
	it means it is ready to listen request, copy that request url(http://<<yourmachineurl>>/api/SubmitRewardsRequest) into your browser and hit enter,
	then it will start to debugging and you can check that by putting breakpoint.

-----------------------------------------------------------------------------------------------------------------------------------------------------------------------------

 RUN ANGULAR 4 Web App Project

1. open the "JE.RMS.WebApp" project in command prompt, right the below command
	>npm install -g typings
2. globally install typescript, if you haven't installed
	>npm install -g typescript
3. install angular cli latest in your machine
	>npm install -g @angular/cli@latest
4. now install all node dependencies defined in package.json which is in root folder of web app project(JE.RMS.WebApp).
	>npm install
5. That's it, now to run you can write.
	>ng serve
	once compilation done successfully, you can open link "http://localhost:4200" on your browser.
6. To build the project to deploy for production
	>ng build -prod

let me know if you face any difficulty.


