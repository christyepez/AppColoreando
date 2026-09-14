import 'package:app_coloreando/src/app.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

void main() {
  testWidgets('renders routed mobile shell', (tester) async {
    await tester.pumpWidget(const ProviderScope(child: AppColoreandoApp()));
    await tester.pumpAndSettle();

    expect(find.text('Colorea tu biblioteca'), findsOneWidget);
    expect(find.text('Catalogo'), findsWidgets);
    expect(find.text('Perfil'), findsWidgets);
    expect(find.text('Ajustes'), findsWidgets);
  });
}
