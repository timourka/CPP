import uuid
from selenium.webdriver.common.by import By
from selenium.webdriver.support import expected_conditions as EC


def test_courses_page_accessible_for_authorized_user(
    driver, wait, base_url, login, user_creds
):
    user_login, user_password = user_creds
    login(user_login, user_password)

    driver.get(f"{base_url}/Courses")
    wait.until(lambda d: "Мои курсы" in d.page_source)

    assert "Мои курсы" in driver.page_source


def test_courses_can_create_new_course(driver, wait, base_url, login, user_creds):
    """Создание курса через форму с asp-for='NewCourseName/Description'."""
    user_login, user_password = user_creds
    login(user_login, user_password)

    driver.get(f"{base_url}/Courses")

    course_name = "SeleniumCourse_" + uuid.uuid4().hex[:6]

    # Razor: <input asp-for="NewCourseName" ... />
    name_input = wait.until(
        EC.visibility_of_element_located((By.ID, "NewCourseName"))
    )
    desc_input = driver.find_element(By.ID, "NewCourseDescription")

    name_input.clear()
    name_input.send_keys(course_name)

    desc_input.clear()
    desc_input.send_keys("Курс создан автотестом Selenium (Python).")

    form = name_input.find_element(By.XPATH, "./ancestor::form")
    submit = form.find_element(By.CSS_SELECTOR, "button[type='submit']")
    submit.click()

    # После POST курс должен появиться в списке "Я автор"
    wait.until(lambda d: course_name in d.page_source)
    assert course_name in driver.page_source


def test_course_details_shows_tasks(driver, wait, base_url, login, user_creds):
    """
    Открываем первый курс и проверяем,
    что на странице деталей есть блок 'Задания'.
    Требует, чтобы у пользователя был доступ хотя бы к одному курсу.
    """
    user_login, user_password = user_creds
    login(user_login, user_password)

    driver.get(f"{base_url}/Courses")

    course_link = wait.until(
        EC.element_to_be_clickable((By.CSS_SELECTOR, "a[href*='/Courses/Details']"))
    )
    course_link.click()

    wait.until(EC.url_contains("/Courses/Details"))
    page_source = driver.page_source

    assert "Задания" in page_source
