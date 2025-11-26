import os
from time import sleep
import pytest
from selenium import webdriver
from selenium.webdriver.chrome.service import Service
from selenium.webdriver.common.by import By
from selenium.webdriver.support.ui import WebDriverWait
from selenium.webdriver.support import expected_conditions as EC
from webdriver_manager.chrome import ChromeDriverManager


@pytest.fixture
def base_url():
    # Порт подставь под свой, если менял launchSettings
    return os.getenv("TRP_BASE_URL", "https://localhost:7260")


@pytest.fixture
def user_creds():
    # Должен существовать обычный пользователь в БД
    return (
        os.getenv("TRP_USER_LOGIN", "a"),
        os.getenv("TRP_USER_PASSWORD", "a"),
    )


@pytest.fixture
def admin_creds():
    # И админ с ролью Admin
    return (
        os.getenv("TRP_ADMIN_LOGIN", "admin"),
        os.getenv("TRP_ADMIN_PASSWORD", "admin"),
    )


@pytest.fixture
def driver():
    options = webdriver.ChromeOptions()
    # для дебага можешь закомментировать headless
    #options.add_argument("--headless=new")
    options.add_argument("--window-size=1920,1080")

    driver = webdriver.Chrome(
        service=Service(ChromeDriverManager().install()),
        options=options,
    )
    driver.implicitly_wait(3)
    yield driver
    driver.quit()


@pytest.fixture
def wait(driver):
    return WebDriverWait(driver, 10)


@pytest.fixture
def login(driver, wait, base_url):
    """Хелпер для логина через /Login"""
    def _login(login_value: str, password_value: str):
        driver.get(f"{base_url}/Login")
        wait.until(EC.visibility_of_element_located((By.ID, "Login")))

        login_input = driver.find_element(By.ID, "Login")
        pass_input = driver.find_element(By.ID, "Password")

        login_input.clear()
        login_input.send_keys(login_value)

        pass_input.clear()
        pass_input.send_keys(password_value)
        sleep(1)

        driver.find_element(By.CSS_SELECTOR, "button[type='submit']").click()
        print("logined")
        sleep(1)

    return _login

