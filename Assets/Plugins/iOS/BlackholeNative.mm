// 블랙홀 만들기 iOS 네이티브 기능: 짧은 햅틱과 공유 시트.
#import <UIKit/UIKit.h>

extern UIViewController *UnityGetGLViewController();

extern "C" {

void _BH_Impact(int style)
{
    UIImpactFeedbackStyle s = style <= 0 ? UIImpactFeedbackStyleLight
                            : style == 1 ? UIImpactFeedbackStyleMedium
                                         : UIImpactFeedbackStyleHeavy;
    UIImpactFeedbackGenerator *generator = [[UIImpactFeedbackGenerator alloc] initWithStyle:s];
    [generator prepare];
    [generator impactOccurred];
}

void _BH_Share(const char *text)
{
    NSString *message = [NSString stringWithUTF8String:text ? text : ""];
    UIViewController *root = UnityGetGLViewController();
    UIActivityViewController *sheet = [[UIActivityViewController alloc] initWithActivityItems:@[message] applicationActivities:nil];
    // 아이패드는 팝오버 기준점이 없으면 멈춘다
    sheet.popoverPresentationController.sourceView = root.view;
    sheet.popoverPresentationController.sourceRect = CGRectMake(root.view.bounds.size.width / 2, root.view.bounds.size.height / 2, 1, 1);
    [root presentViewController:sheet animated:YES completion:nil];
}

}
