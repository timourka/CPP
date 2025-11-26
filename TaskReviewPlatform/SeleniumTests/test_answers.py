import uuid
from selenium.webdriver.common.by import By
from selenium.webdriver.support import expected_conditions as EC


def _open_any_task_answers_page(driver, wait, base_url):
    """
    Открывает:
      /Courses -> первый курс -> первое задание -> /Tasks/Answers/{id}
    Требует: в БД есть хотя бы один курс с хотя бы одним заданием.
    """
    driver.get(f"{base_url}/Courses")

    course_link = wait.until(
        EC.element_to_be_clickable((By.CSS_SELECTOR, "a[href*='/Courses/Details']"))
    )
    course_link.click()

    wait.until(EC.url_contains("/Courses/Details"))

    task_link = wait.until(
        EC.element_to_be_clickable((By.CSS_SELECTOR, "a[href*='/Tasks/Answers']"))
    )
    task_link.click()

    wait.until(EC.url_contains("/Tasks/Answers"))


def test_task_answers_create_new_answer(driver, wait, base_url, login, user_creds):
    user_login, user_password = user_creds
    login(user_login, user_password)

    _open_any_task_answers_page(driver, wait, base_url)

    answer_text = "Ответ автотеста (Python) " + uuid.uuid4().hex[:4]

    textarea = wait.until(
        EC.visibility_of_element_located((By.ID, "NewAnswerText"))
    )
    textarea.clear()
    textarea.send_keys(answer_text)

    form = textarea.find_element(By.XPATH, "./ancestor::form")
    submit = form.find_element(By.CSS_SELECTOR, "button[type='submit']")
    submit.click()

    wait.until(lambda d: answer_text in d.page_source)
    assert answer_text in driver.page_source
