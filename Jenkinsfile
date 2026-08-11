pipeline {
    agent { label 'docker-agent' }

    environment {
        TARGET_ENV = 'prod'
    }

    stages {
        stage('Checkout') {
            steps {
                checkout scm
            }
        }

        stage('Inject Secure AppSettings') {
            steps {
                echo 'Pulling secure JsonConfig from Jenkins Config File Provider...'
                // Inject the managed JSON file into the workspace as appsettings.json
                configFileProvider([configFile(fileId: 'landinggooglejson', targetLocation: 'appsettings.json')]) {
                    echo 'Secure appsettings.json placed in workspace.'
                }
            }
        }

        stage('Deploy (ci/run.sh)') {
            steps {
                echo 'Running deployment script...'
                // Make the script executable and run it
                sh 'chmod +x ci/run.sh'
                sh './ci/run.sh'
            }
        }
    }

    post {
        always {
            echo 'Cleaning up workspace...'
            cleanWs()
        }
        success {
            echo 'Build and Deployment Successful!'
        }
        failure {
            echo 'Pipeline failed. Check the logs for details.'
        }
    }
}
