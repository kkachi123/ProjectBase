from pathlib import Path
from reportlab.lib import colors
from reportlab.lib.enums import TA_LEFT
from reportlab.lib.pagesizes import A4
from reportlab.lib.styles import ParagraphStyle, getSampleStyleSheet
from reportlab.lib.units import mm
from reportlab.pdfbase import pdfmetrics
from reportlab.pdfbase.ttfonts import TTFont
from reportlab.platypus import PageBreak, Paragraph, SimpleDocTemplate, Spacer, Table, TableStyle


SOURCE = Path(r"C:\Users\kkachi\Desktop\Code\프로젝트 분석\PlayMaker2_빠른_사용_설명서.md")
OUTPUT = Path(r"C:\Users\kkachi\Desktop\Code\프로젝트 분석\PlayMaker2_빠른_사용_설명서.pdf")
FONT = Path(r"C:\Windows\Fonts\malgun.ttf")


def add_page_number(canvas, doc):
    canvas.saveState()
    canvas.setFont("Malgun", 8)
    canvas.setFillColor(colors.HexColor("#64748B"))
    canvas.drawRightString(A4[0] - 18 * mm, 12 * mm, f"PlayMaker 2 Quick Guide  |  {doc.page}")
    canvas.restoreState()


def p(text, style):
    return Paragraph(text.replace("&", "&amp;").replace("<", "&lt;").replace(">", "&gt;"), style)


def build():
    if not SOURCE.exists():
        raise FileNotFoundError(SOURCE)
    if not FONT.exists():
        raise FileNotFoundError(FONT)
    pdfmetrics.registerFont(TTFont("Malgun", str(FONT)))
    styles = getSampleStyleSheet()
    normal = ParagraphStyle("KNormal", parent=styles["BodyText"], fontName="Malgun", fontSize=9.3, leading=15, textColor=colors.HexColor("#1E293B"), spaceAfter=5)
    h1 = ParagraphStyle("KH1", parent=styles["Title"], fontName="Malgun", fontSize=23, leading=31, textColor=colors.HexColor("#0F172A"), spaceAfter=14)
    h2 = ParagraphStyle("KH2", parent=styles["Heading2"], fontName="Malgun", fontSize=14, leading=21, textColor=colors.HexColor("#0F766E"), spaceBefore=14, spaceAfter=7)
    h3 = ParagraphStyle("KH3", parent=styles["Heading3"], fontName="Malgun", fontSize=11, leading=16, textColor=colors.HexColor("#334155"), spaceBefore=8, spaceAfter=4)
    callout = ParagraphStyle("Callout", parent=normal, backColor=colors.HexColor("#ECFEFF"), borderColor=colors.HexColor("#14B8A6"), borderWidth=0.7, borderPadding=8, spaceBefore=6, spaceAfter=10)
    code = ParagraphStyle("Code", parent=normal, fontName="Malgun", fontSize=8.5, leading=13, backColor=colors.HexColor("#F1F5F9"), borderPadding=7, leftIndent=2, spaceBefore=4, spaceAfter=8)

    story = []
    story.append(p("PlayMaker 2", h1))
    story.append(p("빠른 사용 설명서", ParagraphStyle("Subtitle", parent=normal, fontSize=13, leading=20, textColor=colors.HexColor("#475569"), spaceAfter=18)))
    story.append(p("Unity에서 FSM(State, Action, Event, Transition)을 이용해 게임 로직을 구성하는 기본 흐름을 빠르게 익히기 위한 실습형 안내서입니다.", callout))

    sections = [
        ("1. 핵심 개념", [
            ("State", "오브젝트가 지금 하는 일입니다. 예: Idle, Move, Attack."),
            ("Action", "State 안에서 실제로 실행하는 작업입니다. 예: 이동, 시간 대기, UI 변경."),
            ("Event", "다음 State로 갈 시점을 알리는 신호입니다. 예: DEAD, FINISHED."),
            ("Transition", "특정 Event가 왔을 때 연결된 다음 State로 이동하는 선입니다."),
            ("Variable", "Action이 읽고 쓰는 값입니다. 예: 체력, 속도, 대상 GameObject."),
        ]),
        ("2. 첫 FSM 만들기", [
            ("1", "Hierarchy에서 대상 GameObject를 선택하고 Add Component에서 PlayMaker FSM을 추가합니다."),
            ("2", "FSM 이름을 기능별로 정합니다. 예: Movement, Health, Combat."),
            ("3", "첫 State를 만들고 Start State로 지정합니다."),
            ("4", "State를 선택한 뒤 Add Action으로 필요한 동작을 추가합니다."),
            ("5", "Transition을 만들고 Event와 다음 State를 연결합니다."),
        ]),
        ("3. 가장 단순한 반복", [
            ("예제", "2초 대기 후 행동하고 다시 대기하는 구조입니다."),
        ]),
        ("4. 변수 선택", [
            ("Local Variable", "한 FSM 내부의 계산값에 사용합니다. 기본 선택입니다."),
            ("Global Variable", "여러 FSM이 의도적으로 공유하는 값에만 씁니다. 예: PlayerXP, CurrentWave."),
            ("Input / Output", "재사용할 Template 또는 FSM의 외부 연결값에 사용합니다."),
        ]),
        ("5. 이벤트 보내기", [
            ("같은 FSM", "Local Event를 사용합니다."),
            ("다른 오브젝트", "Global Event와 Send Event To GameObject FSM을 사용합니다. FSM이 여러 개면 이름을 명시합니다."),
            ("전체 대상", "Broadcast Event는 필요한 경우에만 사용합니다. 예상하지 못한 FSM도 반응할 수 있습니다."),
        ]),
        ("6. Action Browser 빠른 검색", [
            ("이동", "Transform, Move, Translate, Rigidbody"),
            ("시간", "Wait, Timer"),
            ("조건", "Compare, Check, Bool"),
            ("이벤트", "Send Event, Broadcast Event"),
            ("UI", "Text, TMP"),
            ("생성/반환", "Object Pool, Spawn, Release"),
            ("데이터", "DataTable, Get Row Values"),
        ]),
        ("7. DataTable", [
            ("정의", "DataDefinition에서 열을 만들고 DataTableAsset에 행을 입력합니다."),
            ("읽기", "DataTable Get Row Values로 행의 값을 FSM 변수에 저장합니다."),
            ("권장", "게임 흐름은 FSM에, 밸런스 수치와 목록은 DataTable에 둡니다."),
            ("주의", "행을 찾지 못했을 때의 Not Found 처리도 연결합니다."),
        ]),
        ("8. 디버깅", [
            ("1", "Play 모드에서 대상 GameObject를 선택합니다."),
            ("2", "FSM 창에서 현재 활성 State와 전이 흐름을 확인합니다."),
            ("3", "Variables 탭에서 값이 예상대로 변하는지 봅니다."),
            ("4", "Console 오류를 확인합니다."),
        ]),
        ("9. 꼭 지킬 규칙", [
            ("안전", "Scene, Prefab, DataTable, FSM YAML을 직접 편집하지 말고 Unity Editor에서 수정합니다."),
            ("단순성", "한 State에는 한 가지 책임만 둡니다."),
            ("재사용", "새 기능 전에는 기존 Action과 Template을 먼저 검색합니다."),
            ("검증", "변경 후 Play mode, Console, 상태 전이, 변수 값을 확인합니다."),
            ("보호", "Packages/com.hutonggames.playmaker와 ProjectSettings/PlayMaker는 특별한 이유 없이 수정하지 않습니다."),
        ]),
    ]

    for title, rows in sections:
        story.append(p(title, h2))
        if title == "3. 가장 단순한 반복":
            story.append(p("Wait --(2초 경과)--> Jump --(FINISHED)--> Wait", code))
            story.append(p("Wait State에는 Wait Action을, Jump State에는 원하는 동작 Action을 둡니다. 상태가 끝날 때 보내는 Event를 Transition의 이름과 맞춥니다.", normal))
            continue
        table_data = [[p(a, ParagraphStyle("CellHead", parent=normal, textColor=colors.HexColor("#0F766E"), fontSize=8.8)), p(b, normal)] for a, b in rows]
        table = Table(table_data, colWidths=[40 * mm, 132 * mm], hAlign="LEFT")
        table.setStyle(TableStyle([
            ("VALIGN", (0, 0), (-1, -1), "TOP"),
            ("LINEBELOW", (0, 0), (-1, -1), 0.25, colors.HexColor("#CBD5E1")),
            ("LEFTPADDING", (0, 0), (-1, -1), 6),
            ("RIGHTPADDING", (0, 0), (-1, -1), 7),
            ("TOPPADDING", (0, 0), (-1, -1), 5),
            ("BOTTOMPADDING", (0, 0), (-1, -1), 5),
        ]))
        story.append(table)
        story.append(Spacer(1, 3))

    story.append(Spacer(1, 6))
    story.append(p("빠른 기억법", h2))
    story.append(p("State = 지금 무엇을 하는가<br/>Action = 그 상태에서 무엇을 실행하는가<br/>Event = 언제 다음 상태로 갈 것인가<br/>Variable = 동작에 필요한 값은 무엇인가<br/>Transition = 이벤트가 오면 어디로 갈 것인가", callout))

    doc = SimpleDocTemplate(str(OUTPUT), pagesize=A4, rightMargin=18 * mm, leftMargin=18 * mm, topMargin=17 * mm, bottomMargin=18 * mm, title="PlayMaker 2 빠른 사용 설명서", author="Codex")
    doc.build(story, onFirstPage=add_page_number, onLaterPages=add_page_number)


if __name__ == "__main__":
    build()
